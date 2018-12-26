using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AVC;
using AVM;

namespace AVI
{
    class Jxx : IAsseblerVirtualModule
    {
        private Dictionary<string, byte> BaitCodeList;
        private Dictionary<string, byte> RegisterCodes;
        private Dictionary<string, int> RegisterSizes;
        private Dictionary<string, string> Flags;
        private Dictionary<int, string> LocalPointerAddress = new Dictionary<int, string>();
        private Dictionary<string, byte> EBaitCodeList;
        private Dictionary<string, string> EFlags;
        private Dictionary<int, int> ERegSize;
        private Dictionary<string, int> ERegCode;
        private byte[] JMP(string[] args)
        {
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["jmp"]);
            result.AddRange(Commands.CompleteToLenght(BitConverter.GetBytes(0), Convert.ToInt32(Flags["target_bit_depth"]) / 8));
            return result.ToArray();
        }
        private byte[] JE(string[] args)
        {
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["je"]);
            result.AddRange(Commands.CompleteToLenght(BitConverter.GetBytes(0), Convert.ToInt32(Flags["target_bit_depth"]) / 8));
            return result.ToArray();
        }
        private byte[] JNE(string[] args)
        {
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["jne"]);
            result.AddRange(Commands.CompleteToLenght(BitConverter.GetBytes(0), Convert.ToInt32(Flags["target_bit_depth"]) / 8));
            return result.ToArray();
        }
        private byte[] JA(string[] args)
        {
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["ja"]);
            result.AddRange(Commands.CompleteToLenght(BitConverter.GetBytes(0), Convert.ToInt32(Flags["target_bit_depth"]) / 8));
            return result.ToArray();
        }
        private byte[] JB(string[] args)
        {
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["jb"]);
            result.AddRange(Commands.CompleteToLenght(BitConverter.GetBytes(0), Convert.ToInt32(Flags["target_bit_depth"]) / 8));
            return result.ToArray();
        }
        private byte[] JAE(string[] args)
        {
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["jae"]);
            result.AddRange(Commands.CompleteToLenght(BitConverter.GetBytes(0), Convert.ToInt32(Flags["target_bit_depth"]) / 8));
            return result.ToArray();
        }
        private byte[] JBE(string[] args)
        {
            List<byte> result = new List<byte>();
            result.Add(BaitCodeList["jbe"]);
            result.AddRange(Commands.CompleteToLenght(BitConverter.GetBytes(0), Convert.ToInt32(Flags["target_bit_depth"]) / 8));
            return result.ToArray();
        }
        private byte[] JXX(string instruction)
        {
            string[] args = Commands.GetArguments(instruction);
            string command = instruction.Substring(0, instruction.IndexOf(' '));
            switch (command)
            {
                case "jmp":return JMP(args);
                case "je":return JE(args);
                case "jne":return JNE(args);
                case "ja":return JA(args);
                case "jb":return JB(args);
                case "jae":return JAE(args);
                case "jbe":return JBE(args);
            }
            return null;
        }
        public byte[] Compile(string instruction)
        {
            return JXX(instruction);
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
            LocalPointerAddress.Add(address, pointer);
        }

        public bool IsLinkable()
        {
            return true;
        }

        public bool IsRealised(string instruction)
        {
            return instruction == "jmp" ||
                instruction == "je" || 
                instruction == "jne" || 
                instruction == "ja" ||
                instruction == "jb" || 
                instruction == "jae" ||
                instruction == "jbe";
        }

        public void Link(Dictionary<string, int> PointerList, List<byte> Binary)
        {
            foreach (KeyValuePair<int, string> node in LocalPointerAddress)
            {
                byte[] bytePointer = Commands.CompleteToLenght(BitConverter.GetBytes(PointerList[node.Value]), Convert.ToInt32(Flags["target_bit_depth"]) / 8);
                for (int i = 0; i < bytePointer.Length; i++)
                    Binary[node.Key + i] = bytePointer[i];
            }
        }
        public bool IsExecutable(byte baitCode)
        {
            return baitCode == EBaitCodeList["jmp"] ||
                baitCode == EBaitCodeList["je"] ||
                baitCode == EBaitCodeList["jne"] ||
                baitCode == EBaitCodeList["ja"] ||
                baitCode == EBaitCodeList["jb"] ||
                baitCode == EBaitCodeList["jae"] ||
                baitCode == EBaitCodeList["jbe"];
        }

        public void Execute(ref byte[] Registers, ref byte[] RAM)
        {
            int ip = RM.ReadReg(ERegCode["ip"], Registers);
            int pointer = RM.ReadMem(ip + 1, Convert.ToInt32(EFlags["bit_depth"]) / 8, ref RAM);
            int sf = RM.ReadReg(ERegCode["sf"], Registers);
            int zf = RM.ReadReg(ERegCode["zf"], Registers);
            if (RAM[ip] == EBaitCodeList["jmp"]) RM.WriteReg(pointer, ERegCode["ip"], ref Registers);
            else if (RAM[ip] == EBaitCodeList["je"] &&
                zf == 1)
                RM.WriteReg(pointer, ERegCode["ip"], ref Registers);
            else if (RAM[ip] == EBaitCodeList["jne"] &&
                zf == 0)
                RM.WriteReg(pointer, ERegCode["ip"], ref Registers);
            else if (RAM[ip] == EBaitCodeList["ja"] &&
                zf == 0 &&
                sf == 0)
                RM.WriteReg(pointer, ERegCode["ip"], ref Registers);
            else if (RAM[ip] == EBaitCodeList["jb"] &&
                zf == 0 &&
                sf == 1)
                RM.WriteReg(pointer, ERegCode["ip"], ref Registers);
            else if (RAM[ip] == EBaitCodeList["jbe"] &&
                (
                (zf == 0 && sf == 1) ||
                (zf == 1)
                ))
                RM.WriteReg(pointer, ERegCode["ip"], ref Registers);
            else if (RAM[ip] == EBaitCodeList["jae"] &&
                (
                (zf == 0 && sf == 0) ||
                (zf == 1)
                ))
                RM.WriteReg(pointer, ERegCode["ip"], ref Registers);
            else
                RM.WriteReg(ip + 1 + (Convert.ToInt32(EFlags["bit_depth"]) / 8), ERegCode["ip"], ref Registers);

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