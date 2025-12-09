using System;

namespace XProtocol.Serializator
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class XFieldAttribute : Attribute
    {
        public byte FieldID { get; }

        public XFieldAttribute(byte fieldId)
        {
            FieldID = fieldId;
        }
    }
}
