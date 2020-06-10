using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KotORVR
{
	public class _2DAObject
	{
		private List<string> columnNames;
		private List<Dictionary<int, string>> data;

		public string this[int index, string name] {
			get {
				string value = data[index][columnNames.IndexOf(name)];
				if (value == "" || value == "****") {
					return null;
				}
				return value; 
			}
		}

		public _2DAObject(Stream stream)
		{
			byte[] buffer = new byte[8];
			stream.Read(buffer, 0, 8);

			string header = Encoding.UTF8.GetString(buffer);
			if (header != "2DA V2.b") {
				UnityEngine.Debug.Log("Not a 2DA file");
				return;
			}

			stream.ReadByte();  //New line (0x0A)

			//next follows a string of column names, delineated by a space (0x09) and null terminated
			columnNames = new List<string>();

			char c;
			string s = "";
			while ((c = (char)stream.ReadByte()) != 0x00) {
				if (c == 0x09) {
					columnNames.Add(s);
					s = "";
				} else {
					s += c;
				}
			}

			int columns = columnNames.Count;

			//read the number of rows in the file
			stream.Read(buffer, 0, 4);
			int rows = BitConverter.ToInt32(buffer, 0);

			//row indices are next, delineated by a space (0x09) but not null terminated
			List<string> rowIndices = new List<string>();

			s = "";
			for (int i = 0; i < rows;) {
				c = (char)stream.ReadByte();

				if (c == 0x09) {
					rowIndices.Add(s);
					s = "";
					i++;
				} else {
					s += c;
				}
			}

			//now, for each cell there is a ushort offset into the data table, cells are read left to right, top to bottom
			int cells = rows * columns;

			buffer = new byte[cells * 2];
			stream.Read(buffer, 0, cells * 2);

			ushort[] offsets = new ushort[cells];
			for (int i = 0; i < cells; i++) {
				offsets[i] = BitConverter.ToUInt16(buffer, i * 2);
			}

			stream.Position += 2;	//idk

			//the data table immediately follows the offset list
			long dataOffset = stream.Position;

			//fill up the data structure
			data = new List<Dictionary<int, string>>(rows);
			for (int i = 0, o = 0; i < rows; i++) {
				//each row in the data list is a dictionary where the key is the column name index
				data.Add(new Dictionary<int, string>(columns));
				for (int j = 0; j < columns; j++, o++) {
					//jump to the memory offset for this cell
					stream.Position = dataOffset + offsets[o];

					s = "";
					while ((c = (char)stream.ReadByte()) != 0x00) {
						s += c;
					}

					data[i].Add(j, s);
				}
			}
		}
	}
}
