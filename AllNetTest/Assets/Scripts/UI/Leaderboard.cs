using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Leaderboard : MonoBehaviour
{
    class StrIntComparer : IComparer<(string, int)>
    {
        public int Compare((string, int) x, (string, int) y)
        {
            return y.Item2.CompareTo(x.Item2);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var scores = new List<(string, int)>();
        foreach(var item in GameManager.Instances)
        {
            scores.Add((item.photonView.Owner.NickName, (int)item.score));
        }
        scores.Sort(new StrIntComparer());

        var content = "Leaderboard\n";
        for(int i=0; i<scores.Count; i++)
        {
            content += $"{i+1}. {scores[i].Item1}, {scores[i].Item2}\n";
        }

        this.GetComponent<UnityEngine.UI.Text>().text = content;
    }
}
