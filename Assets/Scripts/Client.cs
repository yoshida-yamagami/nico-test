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

    // �ڑ���̃T�[�o�[����ݒ�
#if DEBUG
    public string host = "127.0.0.1";
    public int port = 20001;
#else
    public string host = "127.0.0.1";
    public int port = 20001;
#endif

    /// <summary>
    /// �J�n���̏���
    /// </summary>
    void Start()
    {
        // ���[�J�[�X���b�h���烁�C���X���b�h�ɏ�����n������
        context = SynchronizationContext.Current;

        // �񓚓��̓t�H�[������f�[�^��������悤�ɂ���
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

        // ��M�p�̏��������[�J�[�X���b�h�ŋN��
        Thread thread = new Thread(new ThreadStart(ReceiveProcess));
        thread.Start();

        isConnected = true; // �ڑ���������ʂ芮��������true�ɍX�V
    }

    /// <summary>
    /// �y���[�J�[�X���b�h�ŋN���z�T�[�o�[����f�[�^����M���邽�߂̏���
    /// </summary>
    private async void ReceiveProcess()
    {
        NetworkStream stream = tcpClient.GetStream();

        while (true)
        {
            // ReadAsync�ŃT�[�o�[����̃��b�Z�[�W����M�ҋ@
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

            // Unity�ŗp�ӂ��Ă��郁�\�b�h�́A���[�J�[�X���b�h���ŋN���s�Ȃ��̂�����B
            // Instantiate�̓��C���X���b�h�łȂ��Ǝ��s�s�B
            // context����āA���[�J�[�X���b�h���烁�C���X���b�h�ɏ������˗�����B
            // TODO: ���̂��R�����g��������������������B�B�v�C��
            context.Post(_ =>
            {
                // UI��ɃR�����g�p�̃I�u�W�F�N�g�𐶐���A�e�L�X�g�̓��e����������
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
        // �T�[�o�[�Ɛڑ��ł��Ă��Ȃ��ꍇ�̓f�[�^�𑗂�Ȃ��悤�ɂ���
        if (!isConnected)
        {
            Debug.Log("�T�[�o�[�Ɛڑ��ł��Ă��Ȃ����ߑ��M�s��");
            ShowSystemMessage("�T�[�o�[�Ɛڑ��ł��Ă��Ȃ����ߑ��M�s��");
        }

        // ���̓t�H�[���̕������ǂݍ���
        string answer = answerText.text;
        if (answer.Length == 0)
        { // �e�L�X�g�����͂���Ă��Ȃ��ꍇ�̓��b�Z�[�W�𑗂�Ȃ�
            return;
        }

        // �T�[�o�[�փ��b�Z�[�W�𑗐M
        byte[] sendBuffer = new byte[1024];
        sendBuffer = Encoding.UTF8.GetBytes(answer);
        NetworkStream stream = tcpClient.GetStream();
        await stream.WriteAsync(sendBuffer, 0, sendBuffer.Length);
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

    /// <summary>
    /// �V�X�e�����b�Z�[�W���\���ɂ���
    /// </summary>
    private void HideSystemMessage()
    {
        systemMessage.SetActive(false);
    }

    /// <summary>
    /// �Q�[���I�����ɃT�[�o�[�Ƃ̐ڑ���ؒf����
    /// ��OnApplicationQuit�́AStart��Update�Ɠ��lUnity���p�ӂ��Ă���Ă��郁�\�b�h�B
    /// �@�I�����Ɏ����I�ɌĂ΂��B
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
}
