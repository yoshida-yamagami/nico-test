using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Comment : MonoBehaviour
{
    private int speedX = -1; // �R�����g�̗����X�s�[�h
    private int destroyLineX = -1000; // �����܂ňړ�������R�����g�I�u�W�F�N�g���폜

    void Update()
    {
        // �R�����g�����Ɍ����ė�������
        this.gameObject.transform.position += new Vector3(speedX, 0, 0);

        // �ݒ�ʒu�܂ňړ�������R�����g�I�u�W�F�N�g��j��
        if (this.gameObject.transform.position.x <= destroyLineX)
        {
            Destroy(this.gameObject);
        }
    }
}
