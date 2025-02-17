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

    SynchronizationContext context; // ���[�J�[�X���b�h���烁�C���X���b�h�ɏ�����n���̂Ɏg�p����
    TcpClient tcpClient;
    bool isConnected = false; // �T�[�o�[�Ƃ̐ڑ���������ʂ�I�������true�ɂ���

    // �T�[�o�[�̏���ݒ�
#if DEBUG
    public string host = "127.0.0.1";
    public int port = 20001;
#else
    public string host = "127.0.0.1";
    public int port = 20001;
#endif

    /// <summary>
    /// �Q�[���H�J�n���̏���
    /// </summary>
    void Start()
    {
        // ���[�J�[�X���b�h���烁�C���X���b�h�ɏ�����n������
        context = SynchronizationContext.Current;

        // �񓚓��̓t�H�[������f�[�^�������悤�ɂ���
        answerText = GameObject.Find("Comment").GetComponent<Text>();

        // �T�[�o�[�Ɛڑ�
        ConnectServer();
    }

    /// <summary>
    /// �T�[�o�[�Ƃ̐ڑ�����
    /// </summary>
    async void ConnectServer()
    {
        // �N���C�A���g�쐬
        tcpClient = new TcpClient();

        // ����M�^�C���A�E�g�ݒ�imsec�j
        tcpClient.SendTimeout = 1000;
        tcpClient.ReceiveTimeout = 1000;

        // �T�[�o�[�֐ڑ��v��
        await tcpClient.ConnectAsync(host, port);

        // �T�[�o�[���烁�b�Z�[�W����M
        byte[] sendBuffer = new byte[1024];
        NetworkStream stream = tcpClient.GetStream();
        int length = await stream.ReadAsync(sendBuffer, 0, sendBuffer.Length);
        string receiveString = Encoding.UTF8.GetString(sendBuffer, 0, length);

        // �V�X�e�����b�Z�[�W�Ƃ��ĕ\��
        ShowSystemMessage(receiveString);

        // ��M�p�̃X���b�h�N��
        Thread thread = new Thread(new ParameterizedThreadStart(ReceiveProcess));
        thread.Start(tcpClient);

        isConnected = true; // �ڑ���������ʂ芮��������true�ɍX�V
    }

    /// <summary>
    /// �y�X���b�h�N���z��z�T�[�o�[����̎�M�p�̏���
    /// </summary>
    private async void ReceiveProcess(object value)
    {
        TcpClient tcpClient = (TcpClient)value;
        NetworkStream stream = tcpClient.GetStream();

        while (true)
        {
            // �T�[�o�[���烁�b�Z�[�W����M
            byte[] receiveBuffer = new byte[1024];
            int length = await stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);

            // length��0�Ȃ�ڑ�����Ă��Ȃ��̂ŏ����I��
            if (length <= 0)
            {
                break;
            }

            // �󂯎�����f�[�^�𕶎���ɕϊ�
            string receiveString = Encoding.UTF8.GetString(receiveBuffer, 0, length);
            Debug.Log($"��M������: {receiveString}");

            // Unity�ŗp�ӂ��Ă���API�́A�X���b�h���ŋN���s�Ȃ��̂�����B
            // Instantiate�̓��C���X���b�h�łȂ��Ǝ��s�s�B
            // ���̏������ŁA���C���X���b�h�Ō�قǎ��s�����悤�ɂȂ�
            // TODO: ���̂��R�����g��������������������B�B�v�C��
            context.Post(_ =>
            {
                // UI��ɃR�����g�𐶐�
                GameObject comment = Instantiate(commentPrefab, parentObject.transform.position, Quaternion.identity, parentObject.transform);
                comment.GetComponent<Text>().text = receiveString;
            }, null);
        }
    }

    /// <summary>
    /// �y�u���M�v�{�^�������ŌĂяo�����\�b�h�z���b�Z�[�W���M
    /// </summary>
    public async void SendComment()
    {
        //// �T�[�o�[�Ɛڑ��ł��Ă��Ȃ��ꍇ�̓f�[�^�𑗂�Ȃ��悤�ɂ���
        //if (!isConnected)
        //{
        //    Debug.Log("�T�[�o�[�Ɛڑ��ł��Ă��Ȃ����ߑ��M�s��");
        //    ShowSystemMessage("�T�[�o�[�Ɛڑ��ł��Ă��Ȃ����ߑ��M�s��");
        //}

        // ���͂��ꂽ���b�Z�[�W��ǂݍ���
        string answer = answerText.text;

        // �T�[�o�[�փ��b�Z�[�W�𑗐M
        byte[] sendBuffer = new byte[1024];
        sendBuffer = Encoding.UTF8.GetBytes(answer);
        NetworkStream stream = tcpClient.GetStream();
        await stream.WriteAsync(sendBuffer, 0, sendBuffer.Length);
    }

    /// <summary>
    /// �Q�[���I�����ɃT�[�o�[�Ƃ̐ڑ���ؒf����
    /// </summary>
    async void OnApplicationQuit()
    {
        // �T�[�o�[�֐ڑ��I���p�̕�����𑗐M
        string sendString = "__end";
        byte[] sendBuffer = new byte[1024];
        sendBuffer = Encoding.UTF8.GetBytes(sendString);
        NetworkStream stream = tcpClient.GetStream();
        await stream.WriteAsync(sendBuffer, 0, sendBuffer.Length);

        // �ڑ��I��
        tcpClient.Close();
    }

    /// <summary>
    /// �V�X�e�����b�Z�[�W���\���ɂ���
    /// </summary>
    private void HideSystemMessage()
    {
        systemMessage.SetActive(false);
    }

    /// <summary>
    /// �V�X�e�����b�Z�[�W��\������
    /// </summary>
    /// <param name="message"></param>
    private void ShowSystemMessage(string message)
    {
        systemMessage.SetActive(true);
        systemMessage.GetComponent<Text>().text = message;
        Invoke(nameof(HideSystemMessage), 2.0f);
    }
}
