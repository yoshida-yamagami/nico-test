using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Comment : MonoBehaviour
{
    private int speedX = -1; // コメントの流れるスピード
    private int destroyLineX = -1000; // ここまで移動したらコメントオブジェクトを削除

    void Update()
    {
        // コメントを左に向けて流したい
        this.gameObject.transform.position += new Vector3(speedX, 0, 0);

        // 設定位置まで移動したらコメントオブジェクトを破棄
        if (this.gameObject.transform.position.x <= destroyLineX)
        {
            Destroy(this.gameObject);
        }
    }
}
