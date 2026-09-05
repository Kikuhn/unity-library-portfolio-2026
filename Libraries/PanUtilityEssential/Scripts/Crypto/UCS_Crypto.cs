using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Pan.Util;
using SitraUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Animations;



//? 암호화 관련 코드가 정리되어있는 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    //? 암호화



    /// <summary>
    /// 암호화 및 복호화 관련 기능을 제공하는 정적 클래스입니다.
    /// </summary>
    public static class SU_Crypto
    {
        /// <summary>
        /// 지정된 키와 초기화 벡터를 사용하여 주어진 데이터를 AES 방식으로 암호화합니다.
        /// </summary>
        /// <param name="data">암호화할 원본 데이터</param>
        /// <param name="key">암호화에 사용할 키</param>
        /// <param name="iv">암호화에 사용할 초기화 벡터</param>
        /// <returns>암호화된 데이터를 포함하는 바이트 배열</returns>
        public static byte[] EncryptAes(byte[] data, byte[] key, byte[] iv)
        {
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                aes.Key = key;
                aes.IV = iv;
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(data, 0, data.Length);
                        cs.FlushFinalBlock();
                        return ms.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// 지정된 키와 초기화 벡터를 사용하여 주어진 암호화 데이터를 AES 방식으로 복호화합니다.
        /// </summary>
        /// <param name="data">복호화할 암호화된 데이터</param>
        /// <param name="key">복호화에 사용할 키</param>
        /// <param name="iv">복호화에 사용할 초기화 벡터</param>
        /// <returns>복호화된 데이터를 포함하는 바이트 배열</returns>
        public static byte[] DecryptAes(byte[] data, byte[] key, byte[] iv)
        {
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                aes.Key = key;
                aes.IV = iv;
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(data, 0, data.Length);
                        cs.FlushFinalBlock();
                        return ms.ToArray();
                    }
                }
            }
        }
    }



    ///======================================================================================================================================================
}