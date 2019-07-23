using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;



namespace ChaoticEncryption
{
   
    public class CAEncryption
    {
        #region Fields

        
        private int _generationCount = 100;

        
        private string _passCode = null;

       
        private float _passCodeSize = 12f;

        
        private CAGrid _caGrid = null;

        #endregion

        #region Constructor

        
        public CAEncryption(String passCode)
        {
            _passCode = passCode;
        }

       
        public CAEncryption(String passCode, int generations, float emSize)
        {
            _passCode = passCode;
            _generationCount = generations;
            _passCodeSize = emSize;
        }

        #endregion

        #region Properties

        
        public int MaximumMessageLength
        {
            get
            {
                if (_caGrid == null)
                    return 0;
                else
                    return _caGrid.KeyByteCount;
            }
        }

       
        public int GridWidth
        {
            get
            {
                if (_caGrid != null)
                    return _caGrid.Width;
                else
                    return 0;
            }
        }

        
        public int GridHeight
        {
            get
            {
                if (_caGrid != null)
                    return _caGrid.Height;
                else
                    return 0;
            }
        }

       
        public string PassCode
        {
            get { return _passCode; }
        }

        
        public float Size
        {
            get { return _passCodeSize; }
        }

        
        public int Generations
        {
            get { return _generationCount; }
        }

        #endregion

        #region Methods

        public void GenerateCellData()
        {
            
            _caGrid = new CAGrid();
            _caGrid.BuildFromPassCode(_passCode, _passCodeSize);

            var s = Stopwatch.StartNew();
           
            while (_caGrid.Generation < _generationCount)
                _caGrid.RunFredkinRule();
            s.Stop();
            Debug.Print(s.ElapsedMilliseconds.ToString());

        }


        
        public Bitmap GetCABitmap()
        {
            return _caGrid.GenerateBitmap();
        }

        
        public String EncryptString(String toEncrypt)
        {
            return SimpleBinaryEncoder.Encode(toEncrypt, _caGrid.GetBytes(toEncrypt.Length));
        }

     
        public String DecryptString(String toDecrypt)
        {
            return SimpleBinaryEncoder.Decode(toDecrypt, _caGrid.GetBytes(toDecrypt.Length));
        }

        #endregion

        #region Static Implementations

       
        public static void GenerateRandomFile(string fileName, int length, int generations, int seed)
        {
            using (FileStream fs = File.Create(fileName))
            {
                GenerateRandomFile(fs, length, seed, 256, 256, generations);
            }
        }

       
        public static void GenerateRandomFile(
            Stream outputStream, int lengthBytes, int seed, int gridWidth, int gridHeight, int generationsPerBlock)
        {
            
            CAGrid grid = new CAGrid();

          
            grid.BuildFromPseudoRandomNumbers(seed, gridWidth, gridHeight);

            int written = 0;
            int remaining = lengthBytes;

           
            while (written < lengthBytes)
            {
              
                for (int i = 0; i < generationsPerBlock; i++)
                    grid.RunFredkinRule();

             
                byte[] buffer = grid.GetBytes();

                
                int toWrite = buffer.Length;

               
                if (remaining < buffer.Length)
                    toWrite = remaining;

                outputStream.Write(buffer, 0, toWrite);

                
                written += toWrite;

                
                remaining = lengthBytes - written;

                
                Debug.Print("Written:" + written + " bytes, remaining: " + remaining);
            }
        }


        
        public static event EventHandler<ProgressEventArgs> Progress;

        
        public static void OnProgress(long total, long written, long remaining)
        {
            if (Progress != null)
            {
                Progress(typeof(CAEncryption),new ProgressEventArgs() {
                     Length = total,
                     Written = written,
                     Remaining = remaining
                });
            }
        }

        
        public static IEnumerable<byte> GenerateKeyData(
            long lengthBytes, int seed, int gridWidth, int gridHeight, int generationsPerBlock)
        {
           
            CAGrid grid = new CAGrid();

            
            grid.BuildFromPseudoRandomNumbers(seed, gridWidth, gridHeight);

            
            long written = 0;
            long remaining = lengthBytes;

            
            while (written < lengthBytes)
            {
                
                for (int i = 0; i < generationsPerBlock; i++)
                    grid.RunFredkinRule();

                
                byte[] buffer = grid.GetBytes();

              
                long toWrite = buffer.Length;

               
                if (remaining < buffer.Length)
                    toWrite = remaining;

                
                for (int i = 0; i < toWrite; i++)
                    yield return buffer[i];

               
                written += toWrite;

               
                remaining = lengthBytes - written;

                
                Debug.Print("Written:" + written + " bytes, remaining: " + remaining);

                
                OnProgress(lengthBytes, written, remaining);
            }
        }


      
        public static void EncryptDecryptStream(Stream inputStream, Stream outputStream, int seed)
        {
            long byteLen = inputStream.Length;
            long processed = 0;

            
            foreach (byte b in GenerateKeyData(byteLen, seed, 256, 256, 100))
            {
               
                byte inByte = (byte)inputStream.ReadByte();

              
                byte outputByte = (byte)(inByte ^ b);

               
                outputStream.WriteByte(outputByte);

               
                processed++;
            }
        }

       
        public static void CreateEncryptedFile(FileInfo inputFile, String outputFileName, int seed)
        {
           
            EncryptedFile ef = new EncryptedFile(inputFile.FullName);

            
            using (MemoryStream ms = new MemoryStream())
            {
                
                using (FileStream fsInput = inputFile.OpenRead())
                {
                    
                    EncryptDecryptStream(fsInput, ms, seed);
                }

                
                ef.EncryptedFileData = ms.ToArray();
            }

           
            ef.SaveTo(outputFileName);
        }

        
        public static void DecryptFile(String encryptedFileName, int seed)
        {
           
            EncryptedFile ef = EncryptedFile.Open(encryptedFileName);

           
            String outPath   = Path.GetDirectoryName(encryptedFileName);
            String outName   = Path.GetFileName(ef.OriginalFileName);

            
            using (MemoryStream inputStream = new MemoryStream(ef.EncryptedFileData))
            {
                
                using (FileStream outputStream = File.Create(outPath + "\\" + outName))
                {
                    
                    EncryptDecryptStream(inputStream, outputStream, seed);
                }
            }
        }

        #endregion
    }

   
    [Serializable]
    public class EncryptedFile
    {
       
        public EncryptedFile()
        {
        }

       
        public EncryptedFile(String originalFile)
        {
            OriginalFileName = originalFile;
            DateEncrypted = DateTime.Now;
        }

       
        public string OriginalFileName { get; set; }

        
        public DateTime DateEncrypted { get; set; }

       
        public byte[] EncryptedFileData { get; set; }

        
        public void SaveTo(String fileName)
        {
           
            BinaryFormatter formatter = new BinaryFormatter();

          
            using (FileStream fs = File.Create(fileName))
            {
                formatter.Serialize(fs, this);
            }
        }

        
        public static EncryptedFile Open(String fileName)
        {
            BinaryFormatter formatter = new BinaryFormatter();

           
            using (FileStream fs = File.OpenRead(fileName))
            {
               
                object ef = formatter.Deserialize(fs);

              
                if (ef is EncryptedFile)
                    return ef as EncryptedFile;
                else
                    throw new ArgumentException("File: " + fileName + " Incorrect Type");
            }
        }
    }

   
    public class ProgressEventArgs : EventArgs
    {
        public long Length { get; set; }

        public long Written { get; set; }

       
        public long Remaining { get; set; }

       
        public int PercentComplete
        {
            get
            {
                return (int)(((float)Written/(float)Length) * 100);
            }
        }
    }

}
