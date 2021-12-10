using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

class Scores {
  private Dictionary<string, int> _scores;

  private string _path = Application.dataPath + "/scores/scores.rec";
  private FileStream _fs;
  private BinaryFormatter _fmttr = new BinaryFormatter();

  public Scores() {
    ReadFromFile();
  }
​
  public bool WriteToFile() {
    bool success = false;
​
    _fs = File.Create(_path);
​
    try {
      _fmttr.Serialize(_fs, _scores);
      success = true;
    } catch (System.Exception e) {
      Debug.LogError("Error writing serialized scores to file!");
      Debug.LogError(e.Message);
    }
​
    _fs.Close();
    return success;
  }
​
  public void ReadFromFile() {
    if (!File.Exists(_path)) {
      _fs = File.Create(_path);
      _scores = new Dictionary<string, int>();
    }

    _fs = File.OpenRead(_path);
    _scores = (Dictionary<string, int>)_fmttr.Deserialize(_fs);
    _fs.Close();
  }

  public int AddScore(string name, int score) {
    if (_scores.ContainsKey(name)) {
      if (_scores[name] < score) _scores[name] = score;
    } else {
      _scores[name] = score;
    }
    int numScores = _scores.Count + 1;

    var scores = from entry in _scores orderby entry.Value ascending select entry;

    int rank = 0;
    foreach (var item in scores.OrderByDescending(r => r.Value).Take(numScores)) {
      if (item.Key == name) break;
      rank += 1;
    }
    return rank;
  }

  public Dictionary<string, int> GetTopScores(int numScores) {
    return _scores.OrderByDescending(pair => pair.Value).Take(numScores)
               .ToDictionary(pair => pair.Key, pair => pair.Value);
  }

}