using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DungILModWrapper
{
    public sealed class InjectData
    {
        public bool Enabled { get; set; }

        public string OriginalModuleFullType { get; set; }
        public string OriginalModuleMethodName { get; set; }

        public string NewModuleFullType { get; set; }
        public string NewModuleMethodName { get; set; }

        public bool PlaceBeforeFirstInstruction { get; set; }
        public bool PlaceBeforeLastInstruction { get; set; }
        public bool PlaceBeforeAbsoluteInstruction { get; set; }
        public int AbsoluteIntructionIndex { get; set; }

        public bool MethodCausesReturn { get; set; }
        public bool MethodOverridesReturnValue { get; set; }
        public bool MethodReturnValueIgnoresTypeConstraint { get; set; }

        public InjectData()
        {
            Enabled = false;
            OriginalModuleFullType = "";
            OriginalModuleMethodName = "";
            NewModuleFullType = "";
            NewModuleMethodName = "";
            PlaceBeforeFirstInstruction = false;
            PlaceBeforeLastInstruction = false;
            PlaceBeforeAbsoluteInstruction = false;
            AbsoluteIntructionIndex = 0;
            MethodCausesReturn = false;
            MethodOverridesReturnValue = false;
            MethodReturnValueIgnoresTypeConstraint = false;
        }
    }
}
