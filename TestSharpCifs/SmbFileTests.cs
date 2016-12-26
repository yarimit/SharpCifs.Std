﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpCifs.Smb;
using TestXb;

namespace TestSharpCifs
{
    [TestClass()]
    public class SmbFileTests : TestBase
    {
        private string UserName { get; set; }
        private string Password { get; set; }
        private string ServerName { get; set; }

        public SmbFileTests()
        {
            //this.UserName = "XXXXX";
            //this.Password = "XXXXX";
            //this.ServerName = "XXXXX";
        }

        private string GetUriString(string path)
        {
            return $"smb://{this.UserName}:{this.Password}@{this.ServerName}/{path}";
        }

        private byte[] GetBytes(Stream stream)
        {
            var result = new List<byte>();
            var buffer = new byte[1024];

            while (true)
            {
                var size = stream.Read(buffer, 0, buffer.Length);
                if (size <= 0)
                    break;

                result.AddRange(size == buffer.Length
                    ? buffer
                    : buffer.Take(size));
            }
            return result.ToArray();
        }

        [TestMethod()]
        public void ConnectTest()
        {
            var file = new SmbFile(this.GetUriString("Apps/tmp/test.txt"));
            Assert.IsTrue(file.Exists());
        }

        [TestMethod()]
        public void StreamReadTest()
        {
            var file = new SmbFile(this.GetUriString("Apps/tmp/test.txt"));
            Assert.IsTrue(file.Exists());

            var readStream = file.GetInputStream();
            Assert.AreNotEqual(null, readStream);

            var sjis = Encoding.GetEncoding("Shift_JIS");
            var text = sjis.GetString(this.GetBytes(readStream));
            this.Out(text);
            Assert.IsTrue(text.IndexOf("こんにちは") >= 0);

            readStream.Dispose();
        }

        [TestMethod()]
        public void CreateWriteDeleteTest()
        {
            var a = 1;

            var dir = new SmbFile(this.GetUriString("Apps/tmp/"));
            Assert.IsTrue(dir.Exists());

            var file2 = new SmbFile(dir, "newFile.txt");

            Assert.IsFalse(file2.Exists());

            file2.CreateNewFile();

            Assert.IsTrue(file2.Exists());

            var writeStream = file2.GetOutputStream();
            Assert.AreNotEqual(null, writeStream);

            var textBytes = Encoding.UTF8.GetBytes("マルチバイト\r\n\r\n∀\r\n∀");
            writeStream.Write(textBytes);
            writeStream.Dispose();

            var readStream = file2.GetInputStream();
            Assert.AreNotEqual(null, readStream);

            var text = Encoding.UTF8.GetString(this.GetBytes(readStream));
            this.Out(text);
            Assert.IsTrue(text.IndexOf("バイト") >= 0);
            readStream.Dispose();

            file2.Delete();
            Assert.IsFalse(file2.Exists());
        }

        [TestMethod()]
        public void GetListTest()
        {
            var a = 1;

            var baseDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var dir = new SmbFile(this.GetUriString("Apps/tmp/"));
            Assert.IsTrue(dir.Exists());

            var list = dir.ListFiles();
            foreach (var file in list)
            {
                var name = file.GetName();
                Assert.IsTrue((new string[]{ "taihi.7z", "test.txt", "win10スタートメニュー.txt" }).Contains(name));

                var time = file.LastModified();
                var dateteime = baseDate.AddMilliseconds(time).ToLocalTime();

                this.Out($"Name: {file.GetName()}, isDir?: {file.IsDirectory()}, Date: {dateteime.ToString("yyyy-MM-dd HH:mm:ss")}"); 
            }
        }
    }
}
