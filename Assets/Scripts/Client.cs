using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;

public class Client : MonoBehaviour
{
    [SerializeField] GameObject systemMessage;
    [SerializeField] GameObject commentPrefab;
    [SerializeField] GameObject parentObject;
    Text answerText;

    SynchronizationContext context; // ワーカースレッドからメインスレッドに処理を渡すのに使用する
    TcpClient tcpClient;
    bool isConnected = false; // サーバーとの接続処理が一通り終わったらtrueにする

    // 接続先のサーバー情報を設定
#if DEBUG
    public string host = "127.0.0.1";
    public int port = 20001;
#else
    public string host = "127.0.0.1";
    public int port = 20001;
#endif

    /// <summary>
    /// 開始時の処理
    /// </summary>
    void Start()
    {
        // ワーカースレッドからメインスレッドに処理を渡す準備
        context = SynchronizationContext.Current;

        // 回答入力フォームからデータを見られるようにする
        answerText = GameObject.Find("Comment").GetComponent<Text>();

        // サーバーと接続
        ConnectServer();
    }

    /// <summary>
    /// サーバーとの接続処理
    /// </summary>
    async void ConnectServer()
    {
        // クライアント作成
        tcpClient = new TcpClient();

        // 送受信タイムアウト設定（msec）
        tcpClient.SendTimeout = 1000;
        tcpClient.ReceiveTimeout = 1000;

        // サーバーへ接続要求
        await tcpClient.ConnectAsync(host, port);

        // サーバーからメッセージを受信
        byte[] sendBuffer = new byte[1024];
        NetworkStream stream = tcpClient.GetStream();
        int length = await stream.ReadAsync(sendBuffer, 0, sendBuffer.Length);
        string receiveString = Encoding.UTF8.GetString(sendBuffer, 0, length);

        // システムメッセージとして表示
        ShowSystemMessage(receiveString);

        // 受信用の処理をワーカースレッドで起動
        Thread thread = new Thread(new ThreadStart(ReceiveProcess));
        thread.Start();

        isConnected = true; // 接続処理が一通り完了したらtrueに更新
    }

    /// <summary>
    /// 【ワーカースレッドで起動】サーバーからデータを受信するための処理
    /// </summary>
    private async void ReceiveProcess()
    {
        NetworkStream stream = tcpClient.GetStream();

        while (true)
        {
            // ReadAsyncでサーバーからのメッセージを受信待機
            byte[] receiveBuffer = new byte[1024];
            int length = await stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);

            // lengthが0なら接続されていないので処理終了
            if (length <= 0)
            {
                break;
            }

            // 受け取ったデータを文字列に変換
            string receiveString = Encoding.UTF8.GetString(receiveBuffer, 0, length);
            Debug.Log($"受信文字列: {receiveString}");

            // Unityで用意しているメソッドは、ワーカースレッド内で起動不可なものもある。
            // Instantiateはメインスレッドでないと実行不可。
            // contextを介して、ワーカースレッドからメインスレッドに処理を依頼する。
            // TODO: 何故かコメントがおかしい動きをする。。要修正
            context.Post(_ =>
            {
                // UI上にコメント用のオブジェクトを生成後、テキストの内容を書き換え
                GameObject comment = Instantiate(commentPrefab, parentObject.transform.position, Quaternion.identity, parentObject.transform);
                comment.GetComponent<Text>().text = receiveString;
            }, null);
        }
    }

    /// <summary>
    /// 【「送信」ボタン押下で呼び出すメソッド】メッセージ送信
    /// </summary>
    public async void SendComment()
    {
        // サーバーと接続できていない場合はデータを送らないようにする
        if (!isConnected)
        {
            Debug.Log("サーバーと接続できていないため送信不可");
            ShowSystemMessage("サーバーと接続できていないため送信不可");
        }

        // 入力フォームの文字列を読み込み
        string answer = answerText.text;
        if (answer.Length == 0)
        { // テキストが入力されていない場合はメッセージを送らない
            return;
        }

        // サーバーへメッセージを送信
        byte[] sendBuffer = new byte[1024];
        sendBuffer = Encoding.UTF8.GetBytes(answer);
        NetworkStream stream = tcpClient.GetStream();
        await stream.WriteAsync(sendBuffer, 0, sendBuffer.Length);
    }

    /// <summary>
    /// システムメッセージを表示する
    /// </summary>
    /// <param name="message"></param>
    private void ShowSystemMessage(string message)
    {
        systemMessage.SetActive(true);
        systemMessage.GetComponent<Text>().text = message;
        Invoke(nameof(HideSystemMessage), 2.0f);
    }

    /// <summary>
    /// システムメッセージを非表示にする
    /// </summary>
    private void HideSystemMessage()
    {
        systemMessage.SetActive(false);
    }

    /// <summary>
    /// ゲーム終了時にサーバーとの接続を切断する
    /// ※OnApplicationQuitは、StartやUpdateと同様Unityが用意してくれているメソッド。
    /// 　終了時に自動的に呼ばれる。
    /// </summary>
    async void OnApplicationQuit()
    {
        // サーバーへ接続終了用の文字列を送信
        string sendString = "__end";
        byte[] sendBuffer = new byte[1024];
        sendBuffer = Encoding.UTF8.GetBytes(sendString);
        NetworkStream stream = tcpClient.GetStream();
        await stream.WriteAsync(sendBuffer, 0, sendBuffer.Length);

        // 接続終了
        tcpClient.Close();
    }
}
