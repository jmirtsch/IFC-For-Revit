using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitIFCTools.ParameterExpr
{
   public static class Logger
   {
      private static MemoryStream m_mStream = null;
      public static MemoryStream loggerStream
      {
         get
         {
            if (m_mStream == null) m_mStream = new MemoryStream();
            return m_mStream;
         }
      }

      public static void resetStream()
      {
         if (m_mStream != null)
         {
            m_mStream.Dispose();
            m_mStream = null;
         }
      }

      //        public static MemoryStream mStream = new MemoryStream();

      public static void writeLog(string msgText)
      {
         if (m_mStream == null) m_mStream = new MemoryStream();
         UnicodeEncoding uniEncoding = new UnicodeEncoding();

         byte[] msgString = uniEncoding.GetBytes(msgText);
         m_mStream.Write(msgString, 0, msgString.Length);
         m_mStream.Flush();
      }

      public static char[] getmStreamContent()
      {
         char[] charArray;
         UnicodeEncoding uniEncoding = new UnicodeEncoding();

         byte[] byteArray = new byte[m_mStream.Length];
         int countC = uniEncoding.GetCharCount(byteArray);
         int countB = (int)m_mStream.Length;
         m_mStream.Seek(0, SeekOrigin.Begin);
         m_mStream.Read(byteArray, 0, countB);
         charArray = new char[countC];
         uniEncoding.GetDecoder().GetChars(byteArray, 0, countB, charArray, 0);

         return charArray;
      }
   }
}
