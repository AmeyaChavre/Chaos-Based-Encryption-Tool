using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections;
using System.Security.Cryptography;
using System.IO;
using System.Drawing.Drawing2D;



namespace ChaoticEncryption
{

    
    public class Cell
    {
       
        public Cell(int x, int y, bool state)
        {
            X = x;
            Y = y;
            State = state;
            NextState = false;
            Neighbors = new List<Cell>();
        }

        #region Public Fields

        
        public int X;

       
        public int Y;

        
        public bool State;

        
        public bool NextState;

        public List<Cell> Neighbors;

        #endregion

        #region Properties

       
        public int LivingNeighbours
        {
            get
            {
                int count = 0;

               
                foreach (var cell in Neighbors)
                    if (cell.State)
                        count++;

                return count;
            }
        }

        #endregion

        #region Methods

       
        public override string ToString()
        {
            return "{" + X + "," + Y + ":" + State + "}";
        }

        
        public void SetState(bool alive)
        {
            State = alive;
        }

        
        public void SetNextState(bool alive)
        {
            NextState = alive;
        }

        
        public void CommitState()
        {
            State = NextState;
            NextState = false;
        }

        #endregion
    }

    
    public class CAGrid
    {
        #region Fields

        private List<Cell> _cells = new List<Cell>();

        private int _width = 0;

        private int _height = 0;

        private int _generation = 0;

        private int[] diffs = { -1, 0, 1 };

        #endregion

        #region Cell Accessor Methods

       
        protected IEnumerable<Cell> selectNeighbourCells(Cell cell)
        {
            int[] diffs = { -1, 0, 1 };
            int cx = cell.X; int cy = cell.Y;

            foreach (int xdiff in diffs)
            {
                foreach (int ydiff in diffs)
                {
                    if (ydiff == 0 && xdiff == 0)
                        continue;

                    int x = cx + xdiff;
                    int y = cy + ydiff;

                    if (x >= 0 && x < _width && y >= 0 && y < _height)
                    {
                        yield return GetCell(x, y);
                    }
                }
            }
        }

       
        protected IEnumerable<int> selectNeighbours(Cell cell)
        {
            
            int cx = cell.X; int cy = cell.Y;

            foreach (int xdiff in diffs)
            {
                foreach (int ydiff in diffs)
                {
                    if (ydiff == 0 && xdiff == 0)
                        continue;

                    int x = cx + xdiff;
                    int y = cy + ydiff;

                    if (x >= 0 && x < _width && y >= 0 && y < _height)
                    {
                        yield return getIndex(x, y);
                    }
                }
            }
        }


       
        protected int getIndex(int x, int y)
        {
            return (x * _height) + y;
        }

        
        public Cell GetCell(int x, int y)
        {
            return _cells[getIndex(x, y)];
        }

        #endregion

        #region Properties

        
        public int KeyByteCount
        {
            get
            {
                int bytes = _cells.Count / 8;
                if (_cells.Count % 8 != 0)
                    bytes++;
                return bytes;
            }
        }

      
        public List<Cell> Cells
        {
            get { return _cells; }
        }

        
        public int Width
        {
            get { return _width; }
        }

       
        public int Height
        {
            get { return _height; }
        }

       
        public int Generation
        {
            get { return _generation; }
        }

        
        public Cell this[int x, int y]
        {
            get { return GetCell(x, y); }
        }

        #endregion

        #region Grid Builders

        
        public void BuildFromPassCode(String passCode, float fsize)
        {
           
            Font font = new Font("Calibri", fsize);

            
            var size = Graphics.FromImage(new Bitmap(1, 1)).MeasureString(passCode, font);

          
            int width = 64; int height = 32;

            if (size.Width > width)
                width = (int)size.Width;
            if (size.Height > height)
                height = (int)size.Height;
            
            
            Bitmap bmp = new Bitmap(width, height);

           
            var g = Graphics.FromImage(bmp);

           
            g.FillRegion(
                new LinearGradientBrush(new Point(0, 0), new Point(width, height), Color.LightGray, Color.DarkGray),
                new Region(new Rectangle(0, 0, width, height))
                );

           
            g.DrawString(passCode, font, new SolidBrush(Color.Black), 0, 0);

           
            BuildFromBitmap(bmp);

        }

        
        public void BuildFromPseudoRandomNumbers(int seed, int width, int height)
        {
            BuildGrid(width, height);
            Random rnd = new Random(seed);
            foreach (var cell in _cells)
            {
                if (rnd.NextDouble() > 0.5)
                    cell.State = true;
            }
        }

       
        public void BuildFromBitmap(Bitmap bmp, float threshold)
        {
           
            BuildGrid(bmp.Width, bmp.Height);

           
            for (int cellid = 0; cellid < _cells.Count; cellid++)
            {
                
                Color cellColor = bmp.GetPixel(_cells[cellid].X, _cells[cellid].Y);

              
                if (cellColor.GetBrightness() > threshold)
                    _cells[cellid].SetState(true);
            }
        }

        
        public void BuildFromBitmap(Bitmap bmp)
        {
            
            BuildGrid(bmp.Width, bmp.Height);

           
            Random rnd = new Random(100);

            
            for (int cellid = 0; cellid < _cells.Count; cellid++)
            {
                
                Color cellColor = bmp.GetPixel(_cells[cellid].X, _cells[cellid].Y);

                
                if (cellColor.GetBrightness() < rnd.NextDouble())
                    _cells[cellid].SetState(true);
            }
        }

        
        private void BuildGrid(int width, int height)
        {
            _width = width; _height = height;

            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                   
                    _cells.Add(new Cell(x,y,false));
                }
            }

            
            foreach (Cell cell in _cells)
            {
                
                var neighbours = selectNeighbourCells(cell);

                foreach (var neigbour in neighbours)
                {
                   
                    cell.Neighbors.Add(neigbour);
                }
            }

        }

        #endregion

        #region CA Rule Processing

        
        private int getLivingNeighbours(int cellid)
        {
            int count = 0;
            foreach (var cell in _cells[cellid].Neighbors)
            {
                if (cell.State)
                    count++;
            }
            return count;
        }

       
        public void RunFredkinRule()
        {
          
            foreach (var cell in _cells)
            {
                if (cell.LivingNeighbours % 2 == 0)
                    cell.NextState = false;
                else
                    cell.NextState = true;
            }
            Commit();
        }

        
        public void RunConwaysRuleByIndex()
        {
           
            for (int cellid = 0; cellid < _cells.Count; cellid++)
            {
                
                Cell cell = _cells[cellid];

               
                int neighbours = getLivingNeighbours(cellid);

               
                if (cell.State && neighbours < 2)
                    _cells[cellid].NextState = false;

                if (cell.State && (neighbours == 2 || neighbours == 3))
                    _cells[cellid].NextState = true;

               
                if (cell.State && neighbours > 3)
                    _cells[cellid].NextState = false;

               
                if (!cell.State && neighbours == 3)
                    _cells[cellid].NextState = true;

            }

          
            Commit();
        }

        
        public void RunConwaysRule()
        {
           
            foreach (var cell in _cells)
            {

                
                int neighbours = cell.LivingNeighbours;

               
                if (cell.State && neighbours < 2)
                    cell.NextState = false;

               
                if (cell.State && (neighbours == 2 || neighbours == 3))
                    cell.NextState = true;

               
                if (cell.State && neighbours > 3)
                    cell.NextState = false;

                
                if (!cell.State && neighbours == 3)
                    cell.NextState = true;

            }

            
            Commit();
        }

        
        private void Commit()
        {
          
            foreach (var cell in _cells)
                cell.CommitState();

            
            _generation++;
        }

        #endregion

        #region Get Key Data Methods

       
        public bool[] GetBinaryCellData()
        {
            return GetBinaryCellData(_cells.Count);
        }

        
        public bool[] GetBinaryCellData(int len)
        {
            bool[] data = new bool[len];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = _cells[i].State;
            }
            return data;
        }

       
        public byte[] GetBytes()
        {
            
            BitArray ba = new BitArray(GetBinaryCellData());

          
            int byteLen = ba.Length / 8;
            if (ba.Length % 8 != 0)
                byteLen++;

           
            byte[] buffer = new byte[byteLen];

            
            ba.CopyTo(buffer, 0);

            
            return buffer;
        }

      
        public byte[] GetBytes(int bytes)
        {
           
            int bitLen = bytes * 8;

            if (bitLen > _cells.Count)
                throw new ArgumentException("Insufficient Cells");

            
            BitArray ba = new BitArray(GetBinaryCellData(bytes * 8));

           
            byte[] buffer = new byte[bytes];

           
            ba.CopyTo(buffer, 0);

            return buffer;
        }

        #endregion

        #region Bitmap Generation

        public Bitmap GenerateBitmap(Color backColour, Color cellColour)
        {
            Bitmap bmp = new Bitmap(_width, _height);
            Graphics.FromImage(bmp).Clear(backColour);
            foreach (var cell in _cells)
            {
                if (cell.State)
                {
                    bmp.SetPixel(cell.X, cell.Y, cellColour);
                }
            }
            return bmp;
        }

        public Bitmap GenerateBitmap()
        {
            return GenerateBitmap(Color.White, Color.Black);
        }

        #endregion
    }

    
    public static class SimpleBinaryEncoder
    {
        
        public static String Encode(String message, byte[] keyData)
        {
            
            if (message.Length > keyData.Length)
                throw new ArgumentException("Insufficient Key Data");
            
           
            byte[] msgBytes = Encoding.ASCII.GetBytes(message);
            byte[] trimData = new byte[msgBytes.Length];

            
            Array.Copy(keyData, trimData, trimData.Length);

           
            BitArray msg = new BitArray(msgBytes);
            BitArray key = new BitArray(trimData);

            
            BitArray output = msg.Xor(key);

        
            output.CopyTo(msgBytes, 0);

           
            return Convert.ToBase64String(msgBytes);
        }

       
        public static String Decode(String message, byte[] keyData)
        {
           
            byte[] msgBytes = Convert.FromBase64String(message);
            byte[] trimData = new byte[msgBytes.Length];

           
            Array.Copy(keyData, trimData, trimData.Length);

            
            BitArray msg = new BitArray(msgBytes);
            BitArray key = new BitArray(trimData);
            BitArray output = msg.Xor(key);

            
            output.CopyTo(msgBytes, 0);

           
            return Encoding.ASCII.GetString(msgBytes);
        }
    }

}
