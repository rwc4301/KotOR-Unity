using fastJSON;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KotORVR
{
	public class GFFStruct
	{
		public enum FieldType
		{
			Byte = 0,
			Char = 1,
			Word = 2,
			Short = 3,
			DWord = 4,
			Int = 5,
			DWord64 = 6,
			Int64 = 7,
			Float = 8,
			Double = 9,
			CExoString = 10,
			ResRef = 11,
			CExoLocString = 12,
			Void = 13,
			Struct = 14,
			List = 15,
			Quaternion = 16,
			Vector3 = 17
		}

		public enum Language
		{
			English = 0,
			French = 1,
			German = 2,
			Italian = 3,
			Spanish = 4,
			Polish = 5,
			Korean = 128,
			ChineseTraditional = 129,
			ChineseSimplified = 130,
			Japanese = 131
		}

		public enum Gender
		{
			Male = 0,
			Female = 1
		}

		public struct CExoLocString
		{
			public struct SubString
			{
				public int strid;
				public string str;
			}

			public uint stringref;
			public uint stringcount;
			public SubString[] strings;
		}

		public string Label { get; private set; }
		public FieldType Type { get; private set; }
		public object Value { get; private set; }
		public uint StructID { get; private set; }

		public GFFStruct(string label, FieldType type, object value)
		{
			Label = label;
			Type = type;
			Value = value;
		}

		public GFFStruct(string label, FieldType type, object value, uint structID)
		{
			Label = label;
			Type = type;
			Value = value;
			StructID = structID;
		}

		public GFFStruct(string json)
		{
			JSON.FillObject(this, json);
		}

		public string ToJSON()
		{
			JSON.RegisterCustomType(typeof(Vector3), JSONSerializeVector3, JSONDeserializeVector3);
			JSON.RegisterCustomType(typeof(Quaternion), JSONSerializeQuaternion, JSONDeserializeQuaternion);

			return JSON.ToNiceJSON(this);
		}

		private static string JSONSerializeVector3(object value)
		{
			return ((Vector3)value).ToString();
		}

		private static object JSONDeserializeVector3(string value)
		{
			return new Vector3();
		}

		private static string JSONSerializeQuaternion(object value)
		{
			return ((Quaternion)value).ToString();
		}


		private static object JSONDeserializeQuaternion(string value)
		{
			return new Quaternion();
		}

		//if this field is of the type 'struct', cast the data value as a dictionary and retrieve the requested child field by key
		public GFFStruct this[string key] {
			get {
				if (Type == FieldType.Struct) {
					return ((Dictionary<string, GFFStruct>)Value)[key];
				}
				else {
					throw new System.Exception(string.Format("Tried to retrieve a child value from a GFF field which is not a struct. Field has label {0} and type {1}.", Label, Type));
				}
			}
			set {
				if (Type == FieldType.Struct) {
					GFFStruct old;
					if (((Dictionary<string, GFFStruct>)Value).TryGetValue(key, out old)) {
						((Dictionary<string, GFFStruct>)Value)[key] = value;
					}
					else {
						((Dictionary<string, GFFStruct>)Value).Add(key, value);
					}
				}
				else {
					throw new System.Exception(string.Format("Tried to set a child value on a GFF field which is not a struct. Field has label {0} and type {1}.", Label, Type));
				}
			}
		}

		//if this field is of the type 'list', cast the data value as an array of fields and retrieve the requested child field by index
		public GFFStruct this[int index] {
			get {
				if (Type == FieldType.List) {
					return ((GFFStruct[])Value)[index];
				}
				else {
					throw new System.Exception(string.Format("Tried to retrieve a child struct from a GFF field which is not a list. Field has label {0} and type {1}.", Label, Type));
				}
			}
			set {
				if (Type == FieldType.Struct) {
					((GFFStruct[])Value)[index] = value;
				}
				else {
					throw new System.Exception(string.Format("Tried to set a child struct on a GFF field which is not a list. Field has label {0} and type {1}.", Label, Type));
				}
			}
		}

		public T GetValue<T>()
		{
			try {
				return (T)Value;
			}
			catch (InvalidCastException) {
				Debug.LogError(string.Format("Failed to cast the specified GFF value, label was {0} and the type is {1}", Label, Value.GetType()));
				return default(T);
			}
		}

		public void SetValue<T>(T value)
		{
			Value = value;
		}
	}
}