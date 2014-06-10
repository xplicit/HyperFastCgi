using System;
using System.Text;

namespace HyperFastCgi.FastCgiProtocol
{
	public class FcgiUtils
	{
		public delegate bool AddHeaderDelegate(string name, string value, bool isHeader, object userData);

		public FcgiUtils ()
		{
		}

		public static void ParseParameters (byte[] data, AddHeaderDelegate func, object userData)
		{
			int dataLength = data.Length;
			int offset = 0;
			int nlen, vlen;
			string name, value;
			//TODO: can encoding change?
			Encoding enc = Encoding.Default;

			while (offset < dataLength) {
				nlen = data [offset++];

				if (nlen >= 0x80) {
					nlen = ((0x7F & nlen) * 0x1000000)
						+ ((int)data [offset++]) * 0x10000
						+ ((int)data [offset++]) * 0x100
						+ ((int)data [offset++]);
				}

				vlen = data [offset++];

				if (vlen >= 0x80) {
					vlen = ((0x7F & vlen) * 0x1000000)
						+ ((int)data [offset++]) * 0x10000
						+ ((int)data [offset++]) * 0x100
						+ ((int)data [offset++]);
				}

				// Do a sanity check on the size of the data.
				if (offset + nlen + vlen > dataLength)
					throw new ArgumentOutOfRangeException ("offset");

				name = enc.GetString (data, offset, nlen);
				offset += nlen;
				value = enc.GetString (data, offset, vlen);
				offset += vlen;

				string header = ReformatHttpHeader (name);
				bool isHeader = !String.IsNullOrEmpty (header);

				//return value 'false' means stop further processing
				if (!func (isHeader? header : name, value, isHeader, userData))
					return;
			}
		}

		private static string ReformatHttpHeader (string name)
		{
			if (name.StartsWith ("HTTP_", StringComparison.Ordinal)) {
				char[] header = new char[name.Length - 5];

				// "HTTP_".Length
				int i = 5;
				bool upperCase = true;

				while (i < name.Length) {
					if (name [i] == '_') {
						header [i - 5] = '-';
						upperCase = true;
					} else {
						header [i - 5] = (upperCase) ? name [i] : char.ToLower (name [i]);
						upperCase = false;
					}
					i++; 
				}

				return new string (header);
			} 

			return String.Empty;
		}

	}
}

