/* 【名　　称】EncodingDetector
 * 【概　　要】文字コード自動判別プログラム
 * 【作　　者】koobar
 * 【利用条件】パブリックドメイン（著作権放棄）
 * ================================================================================
 * ☆これは何をするためのプログラムですか？
 * このクラスは、テキストファイルなどから読み込んだバイナリデータの文字コードを、
 * 自動的に判別するための処理を実装したクラスです。判別可能な文字コードは次の通りです。
 * 
 * 　　UTF-7, UTF-8, UTF-16, UTF-32, ASCII, JIS, Shift-JIS, EUC-JP
 * 
 * 上記の文字コードのうち、JIS, Shift-JIS, EUC-JPの判別は、それぞれの文字コードであると
 * 仮定した場合に出現するマルチバイト文字の文字数に依存しています。そのため、必ずしも
 * 正しく判別できるとは限りませんが、作者のPCにあるテストファイルを用いたテストでは、
 * この手法で正確に判別することができました。
 * 
 * ☆利用条件
 * このプログラムはパブリックドメインで、著作権を放棄しています。
 * ご自身で作成されたプログラムと同様に、自由にお使いください。
 * このプログラムを使用、利用、引用したことにより発生したいかなる損害に対しても、
 * 作者は一切の責任を負わないものとします。*/
using System;
using System.IO;
using System.Text;

namespace TextEncodingDetector
{
    public class EncodingDetector
    {
        /// <summary>
        /// 指定されたパスのファイルの文字コードを判別して返す。
        /// </summary>
        /// <param name="path">ファイルの場所</param>
        /// <returns></returns>
        public static Encoding DetectEncoding(string path)
        {
            return DetectEncoding(File.ReadAllBytes(path));
        }

        /// <summary>
        /// 文字コードを判別して返す。
        /// </summary>
        /// <param name="buffer">判別対象のバイナリデータ</param>
        /// <returns></returns>
        public static Encoding DetectEncoding(byte[] buffer)
        {
            // BOM付きのUTF-8であるか判定する。
            if (IsUtf8(buffer))
            {
                return Encoding.UTF8;
            }

            // BOM付きのUTF-16（ビッグエンディアン）であるか判定する。
            if (IsUtf16BigEndian(buffer))
            {
                return Encoding.BigEndianUnicode;
            }

            // BOM付きのUTF-16（リトルエンディアン）であるか判定する。
            if (IsUtf16LittleEndian(buffer))
            {
                return Encoding.Unicode;
            }

            // BOM付きのUTF-32（ビッグエンディアン）であるか判定する。
            if (IsUtf32BigEndian(buffer))
            {
                return new UTF32Encoding(true, true);
            }

            // BOM付きのUTF-32（ビッグエンディアン）であるか判定する。
            if (IsUtf32LittleEndian(buffer))
            {
                return new UTF32Encoding(false, true);
            }

            // BOM付きのUTF-7であるか判定する。
            if (IsUtf7(buffer))
            {
                return Encoding.UTF7;
            }

            // BOM無しのUTF-8であるか判定する。
            if (IsUtf8WithoutBOM(buffer))
            {
                return new UTF8Encoding(false);
            }

            // ASCIIコードであるか判定する。
            if (IsAscii(buffer))
            {
                return Encoding.ASCII;
            }

            // JIS, Shift-JIS, EUC-JPの3つの文字コードにおける、マルチバイト文字の出現数を取得する。
            var jis = Encoding.GetEncoding("iso-2022-jp");
            var sjis = Encoding.GetEncoding("shift_jis");
            var euc = Encoding.GetEncoding("euc-jp");
            int jisc = CountInCharset(buffer, jis);
            int sjisc = CountInCharset(buffer, sjis);
            int eucc = CountInCharset(buffer, euc);

            // マルチバイト文字の出現数が最も多い文字コードが、正しい文字コードであると推測される。
            int max = Math.Max(Math.Max(jisc, sjisc), eucc);
            if (max == jisc)
            {
                return jis;
            }
            else if (max == sjisc)
            {
                return sjis;
            }
            else if (max == eucc)
            {
                return euc;
            }

            // ここに到達すれば、おそらくBOM無しのUTF-8であると考えられる。
            return new UTF8Encoding(false, true);
        }

        /// <summary>
        /// 与えられたバイナリデータが、BOM付きのUTF-8のデータであるか判定する。
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static bool IsUtf8(byte[] buffer)
        {
            return buffer.Length >= 3 &&
                   buffer[0] == 0xEF &&
                   buffer[1] == 0xBB &&
                   buffer[2] == 0xBF;
        }

        /// <summary>
        /// 与えられたバイナリデータが、BOM付きのUTF-16ビッグエンディアンのデータであるか判定する。
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static bool IsUtf16BigEndian(byte[] buffer)
        {
            return buffer.Length >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF;
        }

        /// <summary>
        /// 与えられたバイナリデータが、BOM付きのUTF-16リトルエンディアンのデータであるか判定する。
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static bool IsUtf16LittleEndian(byte[] buffer)
        {
            return buffer.Length >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE;
        }

        /// <summary>
        /// 与えられたバイナリデータが、BOM付きのUTF-32ビッグエンディアンのデータであるか判定する。
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static bool IsUtf32BigEndian(byte[] buffer)
        {
            return buffer.Length >= 4 && buffer[0] == 0x00 && buffer[1] == 0x00 && buffer[2] == 0xFE && buffer[3] == 0xFF;
        }

        /// <summary>
        /// 与えられたバイナリデータが、BOM付きのUTF-32リトルエンディアンのデータであるか判定する。
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static bool IsUtf32LittleEndian(byte[] buffer)
        {
            return buffer.Length >= 4 && buffer[0] == 0xFF && buffer[1] == 0xFE && buffer[2] == 0x00 && buffer[3] == 0x00;
        }

        /// <summary>
        /// 与えられたバイナリデータが、UTF-7のデータであるか判定する。
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static bool IsUtf7(byte[] buffer)
        {
            if (buffer.Length >= 4)
            {
                if (buffer[0] == 0x2B && buffer[1] == 0x2F && buffer[2] == 0x76)
                {
                    switch (buffer[3])
                    {
                        // UTF-7の場合、2B, 2F, 76の後には、38, 39, 2B, 2Fのうちのいずれかが出現する。
                        case 0x38:
                        case 0x39:
                        case 0x2B:
                        case 0x2F:
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 与えられたバイナリデータが、BOM無しのUTF-8のデータであるか判定する。
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static bool IsUtf8WithoutBOM(byte[] data)
        {
            int i = 0;

            while (i < data.Length)
            {
                if ((data[i] & 0x80) == 0)          // 1バイト文字か？（ASCIIと互換性がある）
                {
                    i += 1;
                }
                else if ((data[i] & 0xE0) == 0xC0)  // 2バイト文字か？
                {
                    if (i + 1 >= data.Length || (data[i + 1] & 0xC0) != 0x80)
                    {
                        return false;
                    }

                    i += 2;
                }
                else if ((data[i] & 0xF0) == 0xE0)  // 3バイト文字か？
                {
                    if (i + 2 >= data.Length || (data[i + 1] & 0xC0) != 0x80 || (data[i + 2] & 0xC0) != 0x80)
                    {
                        return false;
                    }

                    i += 3;
                }
                else if ((data[i] & 0xF8) == 0xF0)  // 4バイト文字か？
                {
                    if (i + 3 >= data.Length || (data[i + 1] & 0xC0) != 0x80 || (data[i + 2] & 0xC0) != 0x80 || (data[i + 3] & 0xC0) != 0x80)
                    {
                        return false;
                    }

                    i += 4;
                }
                else
                {
                    i++;
                }
            }

            return true;
        }

        /// <summary>
        /// 与えられたバイナリデータが、ASCIIコードであるか判定する。<br/>
        /// このメソッドは、必ず、Unicode判定を行い、Unicodeでないと判別された場合に使用すること。
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static bool IsAscii(byte[] buffer)
        {
            foreach (byte b in buffer)
            {
                if (b == 0x1B || b >= 0x80)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 与えられた2バイトが、指定された文字コードにおけるマルチバイト文字であるか判定する。
        /// </summary>
        /// <param name="encoding"></param>
        /// <param name="b1"></param>
        /// <param name="b2"></param>
        /// <returns></returns>
        private static bool IsMultibyteChar(Encoding encoding, byte b1, byte b2)
        {
            byte[] bytes = new byte[] { b1, b2 };
            string str = encoding.GetString(bytes);

            return str.Length == 1 && !char.IsSurrogate(str[0]);
        }

        /// <summary>
        /// 指定された文字コードで指定されたバイナリデータを文字列に変換した場合に出現する、マルチバイト文字の文字数を取得する。
        /// </summary>
        /// <param name="data"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        private static int CountInCharset(byte[] data, Encoding encoding)
        {
            int count = 0;

            try
            {
                for (int i = 0; i < data.Length - 1; i++)
                {
                    // マルチバイト文字であるか判定
                    if (i + 1 < data.Length && IsMultibyteChar(encoding, data[i], data[i + 1]))
                    {
                        count++;
                    }
                }
            }
            catch
            {
                // マルチバイト文字のデコードに失敗した場合、確実にその文字コードではないと断言できる。
                count = 0;
            }

            return count;
        }
    }
}
