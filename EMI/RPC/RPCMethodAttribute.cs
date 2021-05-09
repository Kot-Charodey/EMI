using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace EMI
{
    /// <summary>
    /// ��������� ��� ���� ����� ����� ������������� ���������������
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RpcMethodAttribute : Attribute
	{
        /// <summary>
        /// ������ ����������� ������
        /// </summary>
    	public ushort Address { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address">������ ����������� ������</param>
        public RpcMethodAttribute(ushort address)
   		{
        	Address=address;
    	}
	} 
}