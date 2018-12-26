using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AVC;
using AVM;

namespace AVI
{
    class INT : IAsseblerVirtualModule
    {
        private Dictionary<string, byte> BaitCodeList;
        private Dictionary<string, byte> RegisterCodes;
        private Dictionary<string, int> RegisterSizes;
        private Dictionary<string, string> Flags;
        private Dictionary<string, byte> EBaitCodeList;
        private Dictionary<string, int> ERegCode;
        private Dictionary<int, int> ERegSize;
        private Dictionary<string, string> EFlags;
        public byte[] Compile(string instruction)
        {
            string[] args = Commands.GetArguments(instruction);
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["int"]);
            result.Add(Convert.ToByte(Convert.ToInt32(args[0])));
            return result.ToArray();
        }
        public void Init(Dictionary<string, byte> RegisterCodes_, Dictionary<string, int> RegisterSizes_, Dictionary<string, byte> BaitCodeList_, Dictionary<string, string> Flags_)
        {
            RegisterCodes = RegisterCodes_;
            RegisterSizes = RegisterSizes_;
            BaitCodeList = BaitCodeList_;
            Flags = Flags_;
        }
        public void InitLink(string pointer, int address)
        {
            throw new NotImplementedException();
        }
        public bool IsLinkable()
        {
            return false;
        }
        public bool IsRealised(string instruction)
        {
            return instruction == "int";
        }
        public void Link(Dictionary<string, int> PointerList, List<byte> Binary)
        {
            throw new NotImplementedException();
        }
        public bool IsExecutable(byte baitCode)
        {
            return baitCode == EBaitCodeList["int"];
        }
        private int ReadReg(string reg, byte[] Registers)
        {
            List<byte> byteip = new List<byte>();
            for (int i = 0; i < ERegSize[ERegCode[reg]]; i++)
                byteip.Add(Registers[ERegCode[reg] + i]);
            if (byteip.Count == 1)
                return Convert.ToInt32(byteip[0]);
            return BitConverter.ToInt32(byteip.ToArray(), 0);
        }
        private int ReadReg(int addr, byte[] Registers)
        {
            List<byte> byteip = new List<byte>();
            for (int i = 0; i < ERegSize[addr]; i++)
                byteip.Add(Registers[addr + i]);
            return BitConverter.ToInt32(byteip.ToArray(), 0);
        }
        private void WriteReg(int val, string reg, ref byte[] Registers)
        {
            byte[] newip = BitConverter.GetBytes(val);
            for (int i = 0; i < ERegSize[ERegCode[reg]]; i++)
                Registers[ERegCode[reg] + i] = newip[i];
        }
        private void INT21H02H(ref byte[] Registers, ref byte[] RAM)
        {
            int asciicode = ReadReg("dl", Registers);
            Console.Write((char)asciicode);
        }
        private void INT21H09H(ref byte[] Registers, ref byte[] RAM)
        {
            int addr = RM.ReadReg(ERegCode["ds"], Registers);
            int asciicode = RM.ReadMem(addr, 1, ref RAM);
            int i = 1;
            while(asciicode != 36)
            {
                Console.Write((char)asciicode);
                asciicode = RM.ReadMem(addr + i, 1, ref RAM);
                i++;
            }
        }
        private void INT21H01H(ref byte[] Registers, ref byte[] RAM)
        {
            int asciicode = Console.Read();
            RM.WriteReg(asciicode, "al", ref Registers);
        }
        private void INT21H03H(ref byte[] Registers, ref byte[] RAM)
        {
            int val = RM.ReadReg("edx", Registers);
            string valstr = val.ToString();
            for(int i = 0; i < valstr.Length;i++)
                Console.Write((char)valstr[i]);
            
        }
        private void INT21H04H(ref byte[] Registers, ref byte[] RAM)
        {
            int cr = Console.Read();
            string str = "";
            while (cr == ' ')
                cr = Console.Read();
            while(cr >= '0' && cr <= '9')
            {
                str += (char)cr;
                cr = Console.Read();
            }
            RM.WriteReg(Convert.ToInt32(str), "edx", ref Registers);
        }
        private void INT21HXXH(ref byte[] Registers, ref byte[] RAM)
        {
            int num = ReadReg("ah", Registers);
            switch(num)
            {
                case 1: INT21H01H(ref Registers, ref RAM); break;
                case 2: INT21H02H(ref Registers,ref RAM);break;
                case 3: INT21H03H(ref Registers,ref RAM);break;
                case 4: INT21H04H(ref Registers,ref RAM);break;
                case 9: INT21H09H(ref Registers,ref RAM);break;
            }
            int ip = RM.ReadReg("ip", Registers);
            ip += 2;
            RM.WriteReg(ip, "ip", ref Registers);
        }
        public void Execute(ref byte[] Registers, ref byte[] RAM)
        {
            int ip = ReadReg("ip", Registers);
            byte num = RAM[ip + 1];
            if (num == 21) INT21HXXH(ref Registers, ref RAM);
        }

        public void InitExecute(Dictionary<string, byte> EBaitCodeList, Dictionary<string, int> ERegCode, Dictionary<int, int> ERegSize, Dictionary<string, string> EFlags)
        {
            this.EBaitCodeList = EBaitCodeList;
            this.ERegCode = ERegCode;
            this.ERegSize = ERegSize;
            this.EFlags = EFlags;
            RM.Init(ERegCode, ERegSize);
        }
    }
}