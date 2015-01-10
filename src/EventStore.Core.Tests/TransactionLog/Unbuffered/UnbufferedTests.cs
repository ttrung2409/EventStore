﻿using System;
using System.IO;
using EventStore.Core.TransactionLog.Unbuffered;
using NUnit.Framework;

namespace EventStore.Core.Tests.TransactionLog.Unbuffered
{
    [TestFixture]
    public class UnbufferedTests : SpecificationWithDirectory
    {
        [Test]
        public void when_resizing_a_file()
        {
            var filename = GetFilePathFor(Guid.NewGuid().ToString());
            
            var stream = UnbufferedIOFileStream.Create(filename, FileMode.CreateNew, FileAccess.ReadWrite,
                FileShare.ReadWrite, false, 4096, false, 4096);
            stream.SetLength(4096 *1024);
            stream.Close();
            Assert.AreEqual(4096 * 1024, new FileInfo(filename).Length);
        }

        [Test]
        public void when_writing_less_than_buffer()
        {
            var filename = GetFilePathFor(Guid.NewGuid().ToString());
            var bytes = GetBytes(255);
            using (var stream = UnbufferedIOFileStream.Create(filename, FileMode.CreateNew, FileAccess.ReadWrite,
                FileShare.ReadWrite, false, 4096, false, 4096))
            {
                stream.Write(bytes, 0, bytes.Length);
                Assert.AreEqual(0, new FileInfo(filename).Length);
            }
        }

        [Test]
        public void when_writing_more_than_buffer()
        {
            var filename = GetFilePathFor(Guid.NewGuid().ToString());
            var bytes = GetBytes(9000);
            using (var stream = UnbufferedIOFileStream.Create(filename, FileMode.CreateNew, FileAccess.ReadWrite,
                FileShare.ReadWrite, false, 4096, false, 4096))
            {
                stream.Write(bytes, 0, bytes.Length);
                Assert.AreEqual(4096 * 2, new FileInfo(filename).Length);
                var read = ReadAllBytesShared(filename);
                for (var i = 0; i < 4096*2; i++)
                {
                    Assert.AreEqual(i % 256, read[i]);
                }
            }
        }

        [Test]
        public void when_writing_less_than_buffer_and_closing()
        {
            var filename = GetFilePathFor(Guid.NewGuid().ToString());
            var bytes = GetBytes(255);
            using (var stream = UnbufferedIOFileStream.Create(filename, FileMode.CreateNew, FileAccess.ReadWrite,
                FileShare.ReadWrite, false, 4096, false, 4096))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Close();
                Assert.AreEqual(4096, new FileInfo(filename).Length);
                var read = ReadAllBytesShared(filename);

                for (var i = 0; i < 255; i++)
                {
                    Assert.AreEqual(i%256, read[i]);
                }
            }
        }

        [Test]
        public void when_writing_less_than_buffer_and_seeking()
        {
            var filename = GetFilePathFor(Guid.NewGuid().ToString());
            var bytes = GetBytes(255);
            using (var stream = UnbufferedIOFileStream.Create(filename, FileMode.CreateNew, FileAccess.ReadWrite,
                FileShare.ReadWrite, false, 4096, false, 4096))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                Assert.AreEqual(4096, new FileInfo(filename).Length);
                var read = ReadAllBytesShared(filename);

                for (var i = 0; i < 255; i++)
                {
                    Assert.AreEqual(i % 256, read[i]);
                }
            }
        }


        [Test]
        public void when_writing_more_than_buffer_and_closing()
        {
            var filename = GetFilePathFor(Guid.NewGuid().ToString());
            var bytes = GetBytes(9000);
            using (var stream = UnbufferedIOFileStream.Create(filename, FileMode.CreateNew, FileAccess.ReadWrite,
                FileShare.ReadWrite, false, 4096, false, 4096))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Close();
                Assert.AreEqual(4096*3, new FileInfo(filename).Length);
                var read = File.ReadAllBytes(filename);
                for (var i = 0; i < 9000; i++)
                {
                    Assert.AreEqual(i%256, read[i]);
                }
            }
        }

        [Test]
        public void when_reading_on_aligned_buffer()
        {
            var filename = GetFilePathFor(Guid.NewGuid().ToString());
            MakeFile(filename,20000);
            using (var stream = UnbufferedIOFileStream.Create(filename, FileMode.Open, FileAccess.ReadWrite,
                FileShare.ReadWrite, false, 4096, false, 4096))
            {
                var read = new byte[4096];
                stream.Read(read, 0, 4096);
                for (var i = 0; i < 4096; i++)
                {
                    Assert.AreEqual(i % 256, read[i]);
                }
            }            
        }

        [Test]
        public void when_reading_on_unaligned_buffer()
        {
            var filename = GetFilePathFor(Guid.NewGuid().ToString());
            MakeFile(filename, 20000);
            using (var stream = UnbufferedIOFileStream.Create(filename, FileMode.Open, FileAccess.ReadWrite,
                FileShare.ReadWrite, false, 4096, false, 4096))
            {
                stream.Seek(15, SeekOrigin.Begin);
                var read = new byte[4096];
                stream.Read(read, 0, 4096);
                for (var i = 0; i < 4096; i++)
                {
                    Assert.AreEqual((i + 15) % 256, read[i]);
                }
            }
        }

        [Test]
        public void seek_and_read_on_unaligned_buffer()
        {
            var filename = "C:\\foo\\bar.bin";GetFilePathFor(Guid.NewGuid().ToString());
            MakeFile(filename, 20000);
            using (var stream = UnbufferedIOFileStream.Create(filename, FileMode.Open, FileAccess.ReadWrite,
                FileShare.ReadWrite, false, 4096, false, 4096))
            {
                stream.Seek(4096 + 15, SeekOrigin.Begin);
                var read = new byte[999];
                stream.Read(read, 0, read.Length);
                for (var i = 0; i < read.Length; i++)
                {
                    Assert.AreEqual((i + 15) % 256, read[i]);
                }
            }
        }


        [Test]
        public void seek_current_unimplemented()
        {
            var filename = GetFilePathFor(Guid.NewGuid().ToString());
            
            using (var stream = UnbufferedIOFileStream.Create(filename, FileMode.CreateNew, FileAccess.ReadWrite,
                FileShare.ReadWrite, false, 4096, false, 4096))
            {
                Assert.Throws<NotImplementedException>(() => stream.Seek(0, SeekOrigin.Current));
            }
        }

        [Test]
        public void seek_end_unimplemented()
        {
            var filename = GetFilePathFor(Guid.NewGuid().ToString());
            
            using (var stream = UnbufferedIOFileStream.Create(filename, FileMode.CreateNew, FileAccess.ReadWrite,
                FileShare.ReadWrite, false, 4096, false, 4096))
            {
                Assert.Throws<NotImplementedException>(() => stream.Seek(0, SeekOrigin.End));
            }
        }

        [Test]
        public void seek_write_seek_read_in_buffer()
        {
            var filename = GetFilePathFor(Guid.NewGuid().ToString());
            using (var stream = UnbufferedIOFileStream.Create(filename, FileMode.CreateNew, FileAccess.ReadWrite,
                FileShare.ReadWrite, false, 4096, false, 4096))
            {
                var buffer = GetBytes(255);
                stream.Seek(4096 + 15, SeekOrigin.Begin);
                stream.Write(buffer, 0, buffer.Length);
                stream.Seek(4096 + 15, SeekOrigin.Begin);
                var read = new byte[255];
                stream.Read(read, 0, read.Length);
                for (var i = 0; i < read.Length; i++)
                {
                    Assert.AreEqual(i % 255, read[i]);
                }
            }
        }

        private byte[] ReadAllBytesShared(string filename)
        {
            using (var fs = File.Open(filename,FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var ret = new byte[fs.Length];
                fs.Read(ret, 0, (int) fs.Length);
                return ret;
            }
        }

        private void MakeFile(string filename, int size)
        {
            var bytes = GetBytes(size);
            File.WriteAllBytes(filename, bytes);
        }

        private byte[] GetBytes(int size)
        {
            var ret = new byte[size];
            for (var i = 0; i < size; i++)
            {
                ret[i] = (byte) (i%256);
            }
            return ret;
        }
    }
}