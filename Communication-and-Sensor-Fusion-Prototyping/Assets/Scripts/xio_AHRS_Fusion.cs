using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * @file FusionAhrs.c
 * @author Seb Madgwick
 * @brief The AHRS sensor fusion algorithm to combines gyroscope, accelerometer,
 * and magnetometer measurements into a single measurement of orientation
 * relative to the Earth (NWU convention).
 *
 * The algorithm behaviour is governed by a gain.  A low gain will decrease the
 * influence of the accelerometer and magnetometer so that the algorithm will
 * better reject disturbances causes by translational motion and temporary
 * magnetic distortions.  However, a low gain will also increase the risk of
 * drift due to gyroscope calibration errors.  A typical gain value suitable for
 * most applications is 0.5.
 *
 * The algorithm allows the application to define a minimum and maximum valid
 * magnetic field magnitude.  The algorithm will ignore magnetic measurements
 * that fall outside of this range.  This allows the algorithm to reject
 * magnetic measurements that do not represent the direction of magnetic North.
 * The typical magnitude of the Earth's magnetic field is between 20 uT and
 * 70 uT.
 *
 * The algorithm can be used without a magnetometer.  Measurements of
 * orientation obtained using only gyroscope and accelerometer measurements
 * can be expected to drift in the yaw component of orientation only.  The
 * application can reset the drift in yaw by setting the yaw to a specified
 * angle at any time.
 *
 * The algorithm provides the measurement of orientation as a quaternion.  The
 * library includes functions for converting this quaternion to a rotation
 * matrix and Euler angles.
 *
 * The algorithm also provides a measurement of linear acceleration and Earth
 * acceleration.  Linear acceleration is equal to the accelerometer  measurement
 * with the 1 g of gravity removed.  Earth acceleration is a measurement of
 * linear acceleration in the Earth coordinate frame.
 */

namespace xio_Fusion
{
    //------------------------------------------------------------------------------
    // Classes
    ////------------------------------------------------------------------------------
    // Definitions

    public class FusionAhrs
    {
        public float gain;
        public float minimumMagneticFieldSquared;
        public float maximumMagneticFieldSquared;
        public Quaternion quaternion; // describes the Earth relative to the sensor
        public Vector3 linearAcceleration;
        public float rampedGain;
        public bool zeroYawPending;
    }

    public class RotationMatrix
    {
        public float xx;
        public float xy;
        public float xz;
        public float yx;
        public float yy;
        public float yz;
        public float zx;
        public float zy;
        public float zz;

        public static RotationMatrix identity()
        {
            RotationMatrix m = new RotationMatrix();

            m.xx = 1;
            m.xy = 0;
            m.xz = 0;
            m.yx = 0;
            m.yy = 1;
            m.yz = 0;
            m.zx = 0;
            m.zy = 0;
            m.zz = 1;

            return m;
        }
    }

    public class FusionBias
    {
        public float threshold;
        public float samplePeriod;
        public float filterCoefficient;
        public float stationaryTimer;
        public Vector3 gyroscopeBias;
    }
    //------------------------------------------------------------------------------
    // Functions


    public class Fusion
    {
        public FusionAhrs ahrs;
        private FusionBias _bias;
        private float INITIAL_GAIN = 10f;
        private float INITIALISATION_PERIOD = 3.0f;
        private float STATIONARY_PERIOD = 5.0f;

        public Fusion()
        {
            _bias = new FusionBias();
            FusionBiasInitialise(_bias, .5f, .01f);

            ahrs = new FusionAhrs();
            FusionAhrsInitialise(ahrs, .0001f);
        }

        private void FusionBiasInitialise(FusionBias fusionBias, float threshold, float samplePeriod)
        {
            fusionBias.threshold = threshold;
            fusionBias.samplePeriod = samplePeriod;
            fusionBias.filterCoefficient = (2.0f * Mathf.PI * .02f) * fusionBias.samplePeriod;
            fusionBias.stationaryTimer = 0.0f;
            fusionBias.gyroscopeBias = Vector3.zero;
        }

        public void FusionAhrsInitialise(FusionAhrs fusionAhrs, float gain)
        {
            fusionAhrs.gain = gain;
            fusionAhrs.minimumMagneticFieldSquared = 0.0f;
            fusionAhrs.maximumMagneticFieldSquared = Mathf.Infinity;
            fusionAhrs.quaternion = Quaternion.identity;
            fusionAhrs.linearAcceleration = Vector3.zero;
            fusionAhrs.rampedGain = INITIAL_GAIN;
            fusionAhrs.zeroYawPending = false;
        }

        /**
         * @brief Sets the AHRS algorithm gain.  The gain must be equal or greater than
         * zero.
         * @param gain AHRS algorithm gain.
         */
        public void FusionAhrsSetGain(FusionAhrs fusionAhrs, float gain)
        {
            fusionAhrs.gain = gain;
        }

        /**
         * @brief Sets the minimum and maximum valid magnetic field magnitudes in uT.
         * @param fusionAhrs AHRS algorithm structure.
         * @param minimumMagneticField Minimum valid magnetic field magnitude.
         * @param maximumMagneticField Maximum valid magnetic field magnitude.
         */
        public void FusionAhrsSetMagneticField(FusionAhrs fusionAhrs, float minimumMagneticField, float maximumMagneticField)
        {
            fusionAhrs.minimumMagneticFieldSquared = minimumMagneticField * minimumMagneticField;
            fusionAhrs.maximumMagneticFieldSquared = maximumMagneticField * maximumMagneticField;
        }

        private Vector3 FusionBiasUpdate(FusionBias fusionBias, Vector3 gyroscope)
        {
            gyroscope -= fusionBias.gyroscopeBias;

            // Reset stationary timer if gyroscope not stationary
            
            if ((Mathf.Abs(gyroscope.x) > fusionBias.threshold) || (Mathf.Abs(gyroscope.y) > fusionBias.threshold) || (Mathf.Abs(gyroscope.z) > fusionBias.threshold))
            {
                fusionBias.stationaryTimer = 0.0f;
                return gyroscope;
            }

            // Increment stationary timer while gyroscope stationary
            if (fusionBias.stationaryTimer < STATIONARY_PERIOD)
            {
                fusionBias.stationaryTimer += fusionBias.samplePeriod;
                return gyroscope;
            }

            // Adjust bias if stationary timer has elapsed
            fusionBias.gyroscopeBias += gyroscope * fusionBias.filterCoefficient;

            return gyroscope;
        }

        /**
         * @brief Updates the AHRS algorithm.  This function should be called for each
         * new gyroscope measurement.
         * @param fusionAhrs AHRS algorithm structure.
         * @param gyroscope Gyroscope measurement in degrees per second.
         * @param accelerometer Accelerometer measurement in g.
         * @param magnetometer Magnetometer measurement in uT.
         * @param samplePeriod Sample period in seconds.  This is the difference in time
         * between the current and previous gyroscope measurements.
         */
        public void FusionAhrsRawUpdate(FusionAhrs fusionAhrs, Vector3 r_gyroscope, Vector3 r_accelerometer, Vector3 r_magnetometer, float samplePeriod)
        {
            float _Lsb = 65535;
            Vector3 gyroSense = Vector3.one * 1 / 65.5F;
            Vector3 acclSense = Vector3.one * 1 / 8192F;

            // Get calibrated value for each sensor
            Vector3 calibGyro = FusionCalibrationInertial(
                r_gyroscope / _Lsb, RotationMatrix.identity(),
                gyroSense, Vector3.zero);
            Vector3 calibAccel = FusionCalibrationInertial(
                r_accelerometer / (1000 * _Lsb), RotationMatrix.identity(),
                acclSense, Vector3.zero);
            Vector3 calibMag = FusionCalibrationMagnetic(
                r_magnetometer, RotationMatrix.identity(),
                Vector3.zero);//0.15F * _Lsb);

            // Update gyroscope bias correction algorithm
            _bias.samplePeriod = samplePeriod;
            calibGyro = FusionBiasUpdate(_bias, calibGyro);

            // Update AHRS algorithm
            FusionAhrsUpdate(fusionAhrs, calibGyro, calibAccel, calibMag, samplePeriod);
        }

        public void FusionAhrsUpdate(FusionAhrs fusionAhrs, Vector3 gyroscope, Vector3 accelerometer, Vector3 magnetometer, float samplePeriod)
        {
            Quaternion Q = fusionAhrs.quaternion;

            // Calculate feedback error
            Vector3 halfFeedbackError = Vector3.zero; // scaled by 0.5 to avoid repeated multiplications by 2
            do
            {
                // Abandon feedback calculation if accelerometer measurement invalid
                if ((accelerometer.x == 0.0f) && (accelerometer.y == 0.0f) && (accelerometer.z == 0.0f))
                {
                    break;
                }

                // Calculate direction of gravity assumed by quaternion
                Vector3 halfGravity = new Vector3(Q.x * Q.z - Q.w * Q.y,
                                                  Q.w * Q.x + Q.y * Q.z,
                                                  Q.w * Q.w - 0.5f + Q.z * Q.z); // equal to 3rd column of rotation matrix representation scaled by 0.5

                // Calculate accelerometer feedback error
                halfFeedbackError = Vector3.Cross(accelerometer.normalized, halfGravity);
                // FusionVectorCrossProduct(FusionVectorFastNormalise(accelerometer), halfGravity);

                // Abandon magnetometer feedback calculation if magnetometer measurement invalid
                float magnetometerMagnitudeSquared = magnetometer.sqrMagnitude;
                // FusionVectorMagnitudeSquared(magnetometer);
                if ((magnetometerMagnitudeSquared < fusionAhrs.minimumMagneticFieldSquared) || (magnetometerMagnitudeSquared > fusionAhrs.maximumMagneticFieldSquared))
                {
                    Debug.LogWarning("Abandoning magnetometer feedback calculation!");
                    break;
                }

                // Compute direction of 'magnetic west' assumed by quaternion
                Vector3 halfWest = new Vector3(Q.x * Q.y + Q.w * Q.z,
                                               Q.w * Q.w - 0.5f + Q.y * Q.y,
                                               Q.y * Q.z - Q.w * Q.x); // equal to 2nd column of rotation matrix representation scaled by 0.5

                // Calculate magnetometer feedback error
                halfFeedbackError = halfFeedbackError + Vector3.Cross((Vector3.Cross(accelerometer, magnetometer)).normalized, halfWest);
                // FusionVectorAdd(halfFeedbackError, FusionVectorCrossProduct(FusionVectorFastNormalise(FusionVectorCrossProduct(accelerometer, magnetometer)), halfWest));

            } while (false);

            // Ramp down gain until initialisation complete
            if (fusionAhrs.gain == 0)
            {
                fusionAhrs.rampedGain = 0; // skip initialisation if gain is zero
            }
            float feedbackGain = fusionAhrs.gain;
            if (fusionAhrs.rampedGain > fusionAhrs.gain)
            {
                fusionAhrs.rampedGain -= (INITIAL_GAIN - fusionAhrs.gain) * samplePeriod / INITIALISATION_PERIOD;
                feedbackGain = fusionAhrs.rampedGain;
            }

            // Convert gyroscope to radians per second scaled by 0.5
            Vector3 halfGyroscope = gyroscope * 0.5f * 1.0f * Mathf.Deg2Rad;
            // FusionVectorMultiplyScalar(gyroscope, 0.5f * FusionDegreesToRadians(1.0f));

            // Apply feedback to gyroscope
            halfGyroscope = halfGyroscope + halfFeedbackError * feedbackGain;
            // FusionVectorAdd(halfGyroscope, FusionVectorMultiplyScalar(halfFeedbackError, feedbackGain));

            // Integrate rate of change of quaternion
            fusionAhrs.quaternion = fusionAhrs.quaternion * quaternionMultiplyVector(fusionAhrs.quaternion, (halfGyroscope * samplePeriod));
            // FusionQuaternionAdd(fusionAhrs.quaternion, FusionQuaternionMultiplyVector(fusionAhrs.quaternion, FusionVectorMultiplyScalar(halfGyroscope, samplePeriod)));

            // Normalise quaternion
            fusionAhrs.quaternion.Normalize();

            // Calculate linear acceleration
            Vector3 gravity = new Vector3(2.0f * (Q.x * Q.z - Q.w * Q.y),
                                          2.0f * (Q.w * Q.x + Q.y * Q.z),
                                          2.0f * (Q.w * Q.w - 0.5f + Q.z * Q.z)); // equal to 3rd column of rotation matrix representation
            fusionAhrs.linearAcceleration = accelerometer - gravity;
            // FusionVectorSubtract(accelerometer, gravity);
        }

        private Vector3 FusionCalibrationInertial(Vector3 uncalibrated, RotationMatrix misalignment, Vector3 sensitivity, Vector3 bias)
        {
            return RotMatrixMultiplyVector(misalignment, HadamardProduct((uncalibrated - bias), sensitivity));
        }
        //FusionRotationMatrixMultiplyVector(misalignment, FusionVectorHadamardProduct(FusionVectorSubtract(uncalibrated, bias), sensitivity))

        private Vector3 FusionCalibrationMagnetic(Vector3 uncalibrated, RotationMatrix softIronMatrix, Vector3 hardIronBias)
        {
            return RotMatrixMultiplyVector(softIronMatrix, uncalibrated) - hardIronBias;
        }

        private Vector3 HadamardProduct(Vector3 vectorA, Vector3 vectorB)
        {
            Vector3 result;
            result.x = vectorA.x * vectorB.x;
            result.y = vectorA.y * vectorB.y;
            result.z = vectorA.z * vectorB.z;
            return result;
        }

        private Vector3 RotMatrixMultiplyVector(RotationMatrix R, Vector3 vector)
        {
            Vector3 result;

            result.x = R.xx * vector.x + R.xy * vector.y + R.xz * vector.z;
            result.y = R.yx * vector.x + R.yy * vector.y + R.yz * vector.z;
            result.z = R.zx * vector.x + R.zy * vector.y + R.zz * vector.z;

            return result;
        }

        private Quaternion quaternionMultiplyVector(Quaternion Q, Vector3 V)
        {
            Quaternion result;

            result.w = -Q.x * V.x - Q.y * V.y - Q.z * V.z;
            result.x = Q.w * V.x + Q.y * V.z - Q.z * V.y;
            result.y = Q.w * V.y - Q.x * V.z + Q.z * V.x;
            result.z = Q.w * V.z + Q.x * V.y - Q.y * V.x;

            return result;
        }
        /**
         * @brief Updates the AHRS algorithm.  This function should be called for each
         * new gyroscope measurement.
         * @param fusionAhrs AHRS algorithm structure.
         * @param gyroscope Gyroscope measurement in degrees per second.
         * @param accelerometer Accelerometer measurement in g.
         * @param samplePeriod Sample period in seconds.  This is the difference in time
         * between the current and previous gyroscope measurements.
         */
        /*
        void FusionAhrsUpdateWithoutMagnetometer(FusionAhrs* const fusionAhrs, const FusionVector3 gyroscope, const FusionVector3 accelerometer, const float samplePeriod)
        {

            // Update AHRS algorithm
            FusionAhrsUpdate(fusionAhrs, gyroscope, accelerometer, FUSION_VECTOR3_ZERO, samplePeriod);

            // Zero yaw once initialisation complete
            if (FusionAhrsIsInitialising(fusionAhrs) == true)
            {
                fusionAhrs.zeroYawPending = true;
            }
            else
            {
                if (fusionAhrs.zeroYawPending == true)
                {
                    FusionAhrsSetYaw(fusionAhrs, 0.0f);
                    fusionAhrs.zeroYawPending = false;
                }
            }
        }*/

        /**
         * @brief Gets the quaternion describing the sensor relative to the Earth.
         * @param fusionAhrs AHRS algorithm structure.
         * @return Quaternion describing the sensor relative to the Earth.
         */
        /*
        FusionQuaternion FusionAhrsGetQuaternion(const FusionAhrs* const fusionAhrs)
        {
            return FusionQuaternionConjugate(fusionAhrs.quaternion);
        }*/

        /**
         * @brief Gets the linear acceleration measurement equal to the accelerometer
         * measurement with the 1 g of gravity removed.
         * @param fusionAhrs AHRS algorithm structure.
         * @return Linear acceleration measurement.
         */
        /*
        FusionVector3 FusionAhrsGetLinearAcceleration(const FusionAhrs* const fusionAhrs)
        {
            return fusionAhrs.linearAcceleration;
        }*/

        /**
         * @brief Gets the Earth acceleration measurement equal to linear acceleration
         * in the Earth coordinate frame.
         * @param fusionAhrs AHRS algorithm structure.
         * @return Earth acceleration measurement.
         */
        public Vector3 FusionAhrsGetEarthAcceleration(FusionAhrs fusionAhrs)
        {
            Quaternion Q = fusionAhrs.quaternion;
            Vector3 A = fusionAhrs.linearAcceleration;
            float qwqw = Q.w * Q.w; // calculate common terms to avoid repeated operations
            float qwqx = Q.w * Q.x;
            float qwqy = Q.w * Q.y;
            float qwqz = Q.w * Q.z;
            float qxqy = Q.x * Q.y;
            float qxqz = Q.x * Q.z;
            float qyqz = Q.y * Q.z;
            Vector3 earthAcceleration = new Vector3(2.0f * ((qwqw - 0.5f + Q.x * Q.x) * A.x + (qxqy - qwqz) * A.y + (qxqz + qwqy) * A.z),
                                                    2.0f * ((qxqy + qwqz) * A.x + (qwqw - 0.5f + Q.y * Q.y) * A.y + (qyqz - qwqx) * A.z),
                                                    2.0f * ((qxqz - qwqy) * A.x + (qyqz + qwqx) * A.y + (qwqw - 0.5f + Q.z * Q.z) * A.z));
            // transpose of a rotation matrix representation of the quaternion multiplied with the linear acceleration

            return earthAcceleration;
        }

        /**
         * @brief Reinitialise the AHRS algorithm.
         * @param fusionAhrs AHRS algorithm structure.
         */
        public void FusionAhrsReinitialise(FusionAhrs fusionAhrs)
        {
            fusionAhrs.quaternion = Quaternion.identity;
            fusionAhrs.linearAcceleration = Vector3.zero;
            fusionAhrs.rampedGain = INITIAL_GAIN;
        }

        /**
         * @brief Returns true while the AHRS algorithm is initialising.
         * @param fusionAhrs AHRS algorithm structure.
         * @return True while the AHRS algorithm is initialising.
         */
        public bool FusionAhrsIsInitialising(FusionAhrs fusionAhrs)
        {
            return fusionAhrs.rampedGain > fusionAhrs.gain;
        }

        /**
         * @brief Sets the yaw component of the orientation measurement provided by the
         * AHRS algorithm.  This function can be used to reset drift in yaw when the
         * AHRS algorithm is being used without a magnetometer.
         * @param fusionAhrs AHRS algorithm structure.
         * @param yaw Yaw angle in degrees.
         */
        /*
        void FusionAhrsSetYaw(FusionAhrs* const fusionAhrs, const float yaw)
        {
        #define Q fusionAhrs.quaternion.element // define shorthand label for more readable code
            fusionAhrs.quaternion = FusionQuaternionNormalise(fusionAhrs.quaternion); // quaternion must be normalised accurately (approximation not sufficient)
            const float inverseYaw = atan2f(Q.x * Q.y + Q.w * Q.z, Q.w * Q.w - 0.5f + Q.x * Q.x); // Euler angle of conjugate
            const float halfInverseYawMinusOffset = 0.5f * (inverseYaw - FusionDegreesToRadians(yaw));
            const FusionQuaternion inverseYawQuaternion = {
                .element.w = cosf(halfInverseYawMinusOffset),
                .element.x = 0.0f,
                .element.y = 0.0f,
                .element.z = -1.0f * sinf(halfInverseYawMinusOffset),
            };
            fusionAhrs.quaternion = FusionQuaternionMultiply(inverseYawQuaternion, fusionAhrs.quaternion);
        #undef Q // undefine shorthand label
        }*/
    }
    /**
     * @brief Initialises the AHRS algorithm structure.
     * @param fusionAhrs AHRS algorithm structure.
     * @param gain AHRS algorithm gain.
     */
}

//------------------------------------------------------------------------------
// End of file
