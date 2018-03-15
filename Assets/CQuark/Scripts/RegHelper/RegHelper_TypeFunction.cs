﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CQuark
{
	public class RegHelper_TypeFunction : ICQ_TypeFunction
	{
		Type type;
		Dictionary<int, System.Reflection.MethodInfo> cacheT;//= new Dictionary<string, System.Reflection.MethodInfo>();

		public RegHelper_TypeFunction(Type type)
		{
			this.type = type;
		}
		public virtual CQ_Content.Value New(CQ_Content content, IList<CQ_Content.Value> _params)
		{
			List<Type> types = new List<Type>();
			List<object> objparams = new List<object>();
			foreach (var p in _params)
			{
				types.Add(p.type);
				objparams.Add(p.value);
			}
			CQ_Content.Value value = new CQ_Content.Value();
			value.type = type;
			var con = this.type.GetConstructor(types.ToArray());
			if (con == null)
			{
				value.value = Activator.CreateInstance(this.type);
			}
			else
			{
				value.value = con.Invoke(objparams.ToArray());
			}
			return value;
		}
		public virtual CQ_Content.Value StaticCall(CQ_Content content, string function, IList<CQ_Content.Value> _params)
		{
			return StaticCall(content, function, _params, null);
		}
		public virtual CQ_Content.Value StaticCall(CQ_Content content, string function, IList<CQ_Content.Value> _params, MethodCache cache)
		{
			bool needConvert = false;
			List<object> _oparams = new List<object>();
			List<Type> types = new List<Type>();
			bool bEm = false;
			foreach (CQ_Content.Value p in _params)
			{
				_oparams.Add(p.value);
				if ((SType)p.type != null)
				{
					types.Add(typeof(object));
				}
				else
				{
					if (p.type == null)
					{
						bEm = true;

					}
					types.Add(p.type);
				}
			}
			System.Reflection.MethodInfo targetop = null;
			if (!bEm)
				targetop = type.GetMethod(function, types.ToArray());
			//if (targetop == null && type.BaseType != null)//加上父类型静态函数查找,典型的现象是 GameObject.Destory
			//{
			//    targetop = type.BaseType.GetMethod(function, types.ToArray());
			//}
			if (targetop == null)
			{
				if (function[function.Length - 1] == '>')//这是一个临时的模板函数调用
				{
					int sppos = function.IndexOf('<', 0);
					string tfunc = function.Substring(0, sppos);
					string strparam = function.Substring(sppos + 1, function.Length - sppos - 2);
					string[] sf = strparam.Split(',');
					//string tfunc = sf[0];
					Type[] gtypes = new Type[sf.Length];
					for (int i = 0; i < sf.Length; i++)
					{
						gtypes[i] = CQuark.AppDomain.GetTypeByKeyword(sf[i]).type;
					}
					targetop = FindTMethod(type, tfunc, _params, gtypes);

				}
				if (targetop == null)
				{
					Type ptype = type.BaseType;
					while (ptype != null)
					{
						targetop = ptype.GetMethod(function, types.ToArray());
						if (targetop != null) break;
						var t = CQuark.AppDomain.GetType(ptype);
						try
						{
							return t.function.StaticCall(content, function, _params, cache);
						}
						catch
						{

						}
						ptype = ptype.BaseType;
					}

				}
			}
			if (targetop == null)
			{
				//因为有cache的存在，可以更慢更多的查找啦，哈哈哈哈
				targetop = GetMethodSlow(content, true, function, types, _oparams);
				needConvert = true;
			}

			if (targetop == null)
			{
				throw new Exception("函数不存在function:" + type.ToString() + "." + function);
			}
			if (cache != null)
			{
				cache.info = targetop;
				cache.slow = needConvert;
			}


			CQ_Content.Value v = new CQ_Content.Value();
			v.value = targetop.Invoke(null, _oparams.ToArray());
			v.type = targetop.ReturnType;
			return v;
		}

		public virtual CQ_Content.Value StaticCallCache(CQ_Content content, IList<CQ_Content.Value> _params, MethodCache cache)
		{
			List<object> _oparams = new List<object>();
			List<Type> types = new List<Type>();
			foreach (var p in _params)
			{
				_oparams.Add(p.value);
				if ((SType)p.type != null)
				{
					types.Add(typeof(object));
				}
				else
				{
					types.Add(p.type);
				}
			}
			var targetop = cache.info;
			if (cache.slow)
			{
				var pp = targetop.GetParameters();
				for (int i = 0; i < pp.Length; i++)
				{
					if (i >= _params.Count)
					{
						_oparams.Add(pp[i].DefaultValue);
					}
					else
					{
						if (pp[i].ParameterType != (Type)_params[i].type)
						{
							_oparams[i] = CQuark.AppDomain.GetType(_params[i].type).ConvertTo(content, _oparams[i], pp[i].ParameterType);
						}
					}
				}
			}
			CQ_Content.Value v = new CQ_Content.Value();
			v.value = targetop.Invoke(null, _oparams.ToArray());
			v.type = targetop.ReturnType;
			return v;
		}

		public virtual CQ_Content.Value StaticValueGet(CQ_Content content, string valuename)
		{
			var v = MemberValueGet(content, null, valuename);
			if (v == null)
			{
				if (type.BaseType != null)
				{
					return CQuark.AppDomain.GetType(type.BaseType).function.StaticValueGet(content, valuename);
				}
				else
				{
					throw new NotImplementedException();
				}
			}
			else
			{
				return v;
			}

			//var targetf = type.GetField(valuename);
			//if (targetf != null)
			//{
			//    CQ_Content.Value v = new CQ_Content.Value();
			//    v.value = targetf.GetValue(null);
			//    v.type = targetf.FieldType;
			//    return v;
			//}
			//else
			//{
			//    var methodf = type.GetMethod("get_" + valuename);
			//    if (methodf != null)
			//    {
			//        CQ_Content.Value v = new CQ_Content.Value();
			//        v.value = methodf.Invoke(null, null);
			//        v.type = methodf.ReturnType;
			//        return v;
			//    }
			//    //var targetf = type.GetField(valuename);
			//    //if (targetf != null)
			//    //{
			//    //    CQ_Content.Value v = new CQ_Content.Value();
			//    //    v.value = targetf.GetValue(null);
			//    //    v.type = targetf.FieldType;
			//    //    return v;
			//    //}
			//    else
			//    {
			//        var targete = type.GetEvent(valuename);
			//        if (targete != null)
			//        {
			//            CQ_Content.Value v = new CQ_Content.Value();

			//            v.value = new DeleEvent(null, targete);
			//            v.type = targete.EventHandlerType;
			//            return v;
			//        }
			//    }
			//}
			//if (type.BaseType != null)
			//{
			//    return environment.environment.GetType(type.BaseType).function.StaticValueGet(environment, valuename);
			//}


			//throw new NotImplementedException();
		}

		public virtual bool StaticValueSet(CQ_Content content, string valuename, object value)
		{

			bool b = MemberValueSet(content, null, valuename, value);
			if (!b)
			{
				if (type.BaseType != null)
				{
					CQuark.AppDomain.GetType(type.BaseType).function.StaticValueSet(content, valuename, value);
					return true;
				}
				else
				{

					throw new NotImplementedException();
				}
			}
			else
			{
				return b;
			}
		}


		System.Reflection.MethodInfo FindTMethod(Type type, string func, IList<CQ_Content.Value> _params, Type[] gtypes)
		{
			string hashkey = func + "_" + _params.Count + ":";
			foreach (var p in _params)
			{
				hashkey += p.type.ToString();
			}
			foreach (var t in gtypes)
			{
				hashkey += t.ToString();
			}
			int hashcode = hashkey.GetHashCode();
			if (cacheT != null)
			{
				if (cacheT.ContainsKey(hashcode))
				{
					return cacheT[hashcode];
				}
			}
			//+"~" + (sf.Length - 1).ToString();
			var ms = type.GetMethods();
			foreach (var t in ms)
			{
				if (t.Name == func && t.IsGenericMethodDefinition)
				{
					var pp = t.GetParameters();
					if (pp.Length != _params.Count) continue;
					bool match = true;
					for (int i = 0; i < pp.Length; i++)
					{
						if (pp[i].ParameterType.IsGenericType) continue;
						if (pp[i].ParameterType.IsGenericParameter) continue;
						if (pp[i].ParameterType != (Type)_params[i].type)
						{
							match = false;
							break;
						}
					}
					if (match)
					{
						var targetop = t.MakeGenericMethod(gtypes);


						if (cacheT == null)
							cacheT = new Dictionary<int, System.Reflection.MethodInfo>();
						cacheT[hashcode] = targetop;
						return targetop;
					}
				}
			}
			//targetop = type.GetMethod(tfunc, types.ToArray());
			//var pp =targetop.GetParameters();
			//targetop = targetop.MakeGenericMethod(gtypes);
			return null;
		}
		public virtual CQ_Content.Value MemberCall(CQ_Content content, object object_this, string function, IList<CQ_Content.Value> _params)
		{
			return MemberCall(content, object_this, function, _params, null);
		}
		public virtual CQ_Content.Value MemberCall(CQ_Content content, object object_this, string function, IList<CQ_Content.Value> _params, MethodCache cache)
		{
			bool needConvert = false;
			List<Type> types = new List<Type>();
			List<object> _oparams = new List<object>();
			bool bEm = false;
			foreach (CQ_Content.Value p in _params)
			{
				{
					_oparams.Add(p.value);
				}
				if ((SType)p.type != null)
				{
					types.Add(typeof(object));
				}
				else
				{
					if (p.type == null)
					{
						bEm = true;
					}
					types.Add(p.type);
				}
			}

			System.Reflection.MethodInfo targetop = null;
			if (!bEm)
			{
				targetop = type.GetMethod(function, types.ToArray());
			}
			CQ_Content.Value v = new CQ_Content.Value();
			if (targetop == null)
			{
				if (function[function.Length - 1] == '>')//这是一个临时的模板函数调用
				{
					int sppos = function.IndexOf('<', 0);
					string tfunc = function.Substring(0, sppos);
					string strparam = function.Substring(sppos + 1, function.Length - sppos - 2);
					string[] sf = strparam.Split(',');
					//string tfunc = sf[0];
					Type[] gtypes = new Type[sf.Length];
					for (int i = 0; i < sf.Length; i++)
					{
						gtypes[i] = CQuark.AppDomain.GetTypeByKeyword(sf[i]).type;
					}
					targetop = FindTMethod(type, tfunc, _params, gtypes);
					var ps = targetop.GetParameters();
					for(int i=0;i<Math.Min(ps.Length,_oparams.Count);i++)
					{
						if(ps[i].ParameterType!=(Type)_params[i].type)
						{

							_oparams[i] = CQuark.AppDomain.GetType(_params[i].type).ConvertTo(content, _oparams[i], ps[i].ParameterType);
						}
					}
				}
				else
				{
					if (!bEm)
					{
						foreach (var s in type.GetInterfaces())
						{
							targetop = s.GetMethod(function, types.ToArray());
							if (targetop != null) break;
						}
					}
					if (targetop == null)
					{//因为有cache的存在，可以更慢更多的查找啦，哈哈哈哈
						targetop = GetMethodSlow(content, false, function, types, _oparams);
						needConvert = true;
					}
					if (targetop == null)
					{
						throw new Exception("函数不存在function:" + type.ToString() + "." + function);
					}
				}
			}
			if (cache != null)
			{
				cache.info = targetop;
				cache.slow = needConvert;
			}

			if (targetop == null)
			{
				throw new Exception("函数不存在function:" + type.ToString() + "." + function);
			}
			v.value = targetop.Invoke(object_this, _oparams.ToArray());
			v.type = targetop.ReturnType;
			return v;
		}

		public bool HasFunction(string key){
			//TODO 
			return false;
		}

		public virtual IEnumerator CoroutineCall(CQ_Content content, object object_this, string function, IList<CQ_Content.Value> _params, ICoroutine coroutine)
		{
			//TODO 不存在這樣的調用
			MemberCall(content, object_this, function, _params, null);
			yield return null;
		}

		Dictionary<string, IList<System.Reflection.MethodInfo>> slowCache = null;

		System.Reflection.MethodInfo GetMethodSlow(CQuark.CQ_Content content, bool bStatic, string funcname, IList<Type> types, IList<object> _params)
		{
			List<object> myparams = new List<object>(_params);
			if (slowCache == null)
			{
				System.Reflection.MethodInfo[] ms = this.type.GetMethods();
				slowCache = new Dictionary<string, IList<System.Reflection.MethodInfo>>();
				foreach (var m in ms)
				{
					string name = m.IsStatic ? "s=" + m.Name : m.Name;
					if (slowCache.ContainsKey(name) == false)
					{
						slowCache[name] = new List<System.Reflection.MethodInfo>();
					}
					slowCache[name].Add(m);
				}
			}
			IList<System.Reflection.MethodInfo> minfo = null;

			if (slowCache.TryGetValue(bStatic ? "s=" + funcname : funcname, out minfo) == false)
				return null;

			foreach (var m in minfo)
			{
				bool match = true;
				var pp = m.GetParameters();
				if (pp.Length < types.Count)//参数多出来，不匹配
				{
					match = false;
					continue;
				}
				for (int i = 0; i < pp.Length; i++)
				{
					if (i >= types.Count)//参数多出来
					{
						if (!pp[i].IsOptional)
						{

							match = false;
							break;
						}
						else
						{
							myparams.Add(pp[i].DefaultValue);
							continue;
						}
					}
					else
					{
						if (pp[i].ParameterType == types[i]) continue;

						try
						{
							if (types[i] == null && !pp[i].ParameterType.IsValueType)
							{
								continue;
							}
							myparams[i] = CQuark.AppDomain.GetType(types[i]).ConvertTo(content, _params[i], pp[i].ParameterType);
							if (myparams[i] == null)
							{
								match = false;
								break;
							}
						}
						catch
						{
							match = false;
							break;
						}
					}
					if (match)
						break;
				}
				if (!match)
				{
					continue;
				}
				else
				{
					for (int i = 0; i < myparams.Count; i++)
					{
						if (i < _params.Count)
						{
							_params[i] = myparams[i];
						}
						else
						{
							_params.Add(myparams[i]);
						}
					}
					return m;
				}

			}

			if (minfo.Count == 1)
				return minfo[0];

			return null;

		}
		public virtual CQ_Content.Value MemberCallCache(CQ_Content content, object object_this, IList<CQ_Content.Value> _params, MethodCache cache)
		{
			List<Type> types = new List<Type>();
			List<object> _oparams = new List<object>();
			foreach (var p in _params)
			{
				{
					_oparams.Add(p.value);
				}
				if ((SType)p.type != null)
				{
					types.Add(typeof(object));
				}
				else
				{
					types.Add(p.type);
				}
			}

			var targetop = cache.info;
			CQ_Content.Value v = new CQ_Content.Value();
			if (cache.slow)
			{
				var pp = targetop.GetParameters();
				for (int i = 0; i < pp.Length; i++)
				{
					if (i >= _params.Count)
					{
						_oparams.Add(pp[i].DefaultValue);
					}
					else
					{
						if (pp[i].ParameterType != (Type)_params[i].type)
						{
							_oparams[i] = CQuark.AppDomain.GetType(_params[i].type).ConvertTo(content, _oparams[i], pp[i].ParameterType);
						}
					}
				}
			}
			v.value = targetop.Invoke(object_this, _oparams.ToArray());
			v.type = targetop.ReturnType;
			return v;
		}

		class MemberValueCache
		{
			public int type = 0;
			public System.Reflection.FieldInfo finfo;
			public System.Reflection.MethodInfo minfo;
			public System.Reflection.EventInfo einfo;

		}
		Dictionary<string, MemberValueCache> memberValuegetCaches = new Dictionary<string, MemberValueCache>();
		public virtual CQ_Content.Value MemberValueGet(CQ_Content content, object object_this, string valuename)
		{

			MemberValueCache c;

			if (!memberValuegetCaches.TryGetValue(valuename, out c))
			{
				c = new MemberValueCache();
				memberValuegetCaches[valuename] = c;
				c.finfo = type.GetField(valuename);
				if (c.finfo == null)
				{
					c.minfo = type.GetMethod("get_" + valuename);
					if (c.minfo == null)
					{
						c.einfo = type.GetEvent(valuename);
						if (c.einfo == null)
						{
							c.type = -1;
							return null;
						}
						else
						{
							c.type = 3;
						}
					}
					else
					{
						c.type = 2;
					}
				}
				else
				{
					c.type = 1;
				}
			}

			if (c.type < 0) return null;
			CQ_Content.Value v = new CQ_Content.Value();
			switch (c.type)
			{
			case 1:

				v.value = c.finfo.GetValue(object_this);
				v.type = c.finfo.FieldType;
				break;
			case 2:

				v.value = c.minfo.Invoke(object_this, null);
				v.type = c.minfo.ReturnType;
				break;
			case 3:
				v.value = new DeleEvent(object_this, c.einfo);
				v.type = c.einfo.EventHandlerType;
				break;

			}
			return v;

			//var targetf = type.GetField(valuename);
			//if (targetf != null)
			//{
			//    CQ_Content.Value v = new CQ_Content.Value();
			//    v.value = targetf.GetValue(object_this);
			//    v.type = targetf.FieldType;
			//    return v;
			//}

			//var targetp = type.GetProperty(valuename);
			//if (targetp != null)
			//{
			//    CQ_Content.Value v = new CQ_Content.Value();
			//    v.value = targetp.GetValue(object_this, null);
			//    v.type = targetp.PropertyType;
			//    return v;
			//}
			//else
			//{//用get set 方法替代属性操作，AOT环境属性操作有问题
			//    var methodf = type.GetMethod("get_" + valuename);
			//    if (methodf != null)
			//    {
			//        CQ_Content.Value v = new CQ_Content.Value();
			//        v.value = methodf.Invoke(object_this, null);
			//        v.type = methodf.ReturnType;
			//        return v;
			//    }
			//var targetf = type.GetField(valuename);
			//if (targetf != null)
			//{
			//    CQ_Content.Value v = new CQ_Content.Value();
			//    v.value = targetf.GetValue(object_this);
			//    v.type = targetf.FieldType;
			//    return v;
			//}
			//    else
			//    {
			//        System.Reflection.EventInfo targete = type.GetEvent(valuename);
			//        if (targete != null)
			//        {
			//            CQ_Content.Value v = new CQ_Content.Value();
			//            v.value = new DeleEvent(object_this, targete);
			//            v.type = targete.EventHandlerType;
			//            return v;
			//        }
			//    }
			//}

			//return null;
		}

		Dictionary<string, MemberValueCache> memberValuesetCaches = new Dictionary<string, MemberValueCache>();

		public virtual bool MemberValueSet(CQ_Content content, object object_this, string valuename, object value)
		{
			MemberValueCache c;

			if (!memberValuesetCaches.TryGetValue(valuename, out c))
			{
				c = new MemberValueCache();
				memberValuesetCaches[valuename] = c;
				c.finfo = type.GetField(valuename);
				if (c.finfo == null)
				{
					c.minfo = type.GetMethod("set_" + valuename);
					//                    var mss = type.GetMethods();
					if (c.minfo == null)
					{
						if (type.GetMethod("add_" + valuename) != null)
						{
							c.type = 3;//event;
						}
						else
						{
							c.type = -1;
							return false;
						}
					}

					else
					{
						c.type = 2;
					}
				}
				else
				{
					c.type = 1;
				}
			}

			if (c.type < 0)
				return false;

			if (c.type == 1)
			{
				if (value != null && value.GetType() != c.finfo.FieldType)
				{

					value = CQuark.AppDomain.GetType(value.GetType()).ConvertTo(content, value, c.finfo.FieldType);
				}
				c.finfo.SetValue(object_this, value);
			}
			else if(c.type==2)
			{
				var ptype = c.minfo.GetParameters()[0].ParameterType;
				if (value != null && value.GetType() != ptype)
				{

					value = CQuark.AppDomain.GetType(value.GetType()).ConvertTo(content, value, ptype);
				}
				c.minfo.Invoke(object_this, new object[] { value });
			}
			return true;
			////先操作File
			//var targetf = type.GetField(valuename);
			//if (targetf != null)
			//{
			//    if (value != null && value.GetType() != targetf.FieldType)
			//    {

			//        value = CQuark.AppDomain.GetType(value.GetType()).ConvertTo(content, value, targetf.FieldType);
			//    }
			//    targetf.SetValue(object_this, value);
			//    return;
			//}
			//else
			//{
			//    var methodf = type.GetMethod("set_" + valuename);
			//    if (methodf != null)
			//    {
			//        var ptype = methodf.GetParameters()[0].ParameterType;
			//        if (value != null && value.GetType() != ptype)
			//        {

			//            value = CQuark.AppDomain.GetType(value.GetType()).ConvertTo(content, value, ptype);
			//        }
			//        methodf.Invoke(object_this, new object[] { value });

			//        return;
			//    }
			//}



			//throw new NotImplementedException();
		}



		System.Reflection.MethodInfo indexGetCache = null;
		Type indexGetCachetypeindex;
		Type indexGetCacheType = null;
		public virtual CQ_Content.Value IndexGet(CQ_Content content, object object_this, object key)
		{
			//var m = type.GetMembers();
			if (indexGetCache == null)
			{
				indexGetCache = type.GetMethod("get_Item");
				if (indexGetCache != null)
				{
					indexGetCacheType = indexGetCache.ReturnType;
				}
				//    CQ_Content.Value v = new CQ_Content.Value();
				//    v.type = targetop.ReturnType;
				//    v.value = targetop.Invoke(object_this, new object[] { key });
				//    return v;
				//}
				if (indexGetCache == null)
				{
					indexGetCache = type.GetMethod("GetValue", new Type[] { typeof(int) });
					if (indexGetCache != null)
					{
						indexGetCacheType = type.GetElementType();
					}

				}
				indexGetCachetypeindex = indexGetCache.GetParameters()[0].ParameterType;
				//if (indexGetCache != null)
				//{
				//    CQ_Content.Value v = new CQ_Content.Value();
				//    v.type = indexGetCacheType;
				//    v.value = indexGetCache.Invoke(object_this, new object[] { key });
				//    return v;
				//}
				//{
				//    targetop = type.GetMethod("GetValue", new Type[] { typeof(int) });
				//    if (targetop != null)
				//    {
				//        //targetop = type.GetMethod("Get");

				//        CQ_Content.Value v = new CQ_Content.Value();
				//        v.type = type.GetElementType();
				//        v.value = targetop.Invoke(object_this, new object[] { key });
				//        return v;
				//    }
				//}
			}
			//else
			{
				CQ_Content.Value v = new CQ_Content.Value();
				v.type = indexGetCacheType;
				if (key != null && key.GetType() != indexGetCachetypeindex)
					key = CQuark.AppDomain.GetType(key.GetType()).ConvertTo(content, key, (CQuark.CQType)indexGetCachetypeindex);
				v.value = indexGetCache.Invoke(object_this, new object[] { key });
				return v;
			}
			//throw new NotImplementedException();

		}

		System.Reflection.MethodInfo indexSetCache = null;
		Type indexSetCachetype1;
		Type indexSetCachetype2;
		bool indexSetCachekeyfirst = false;
		public virtual void IndexSet(CQ_Content content, object object_this, object key, object value)
		{
			if (indexSetCache == null)
			{
				indexSetCache = type.GetMethod("set_Item");
				indexSetCachekeyfirst = true;
				if (indexSetCache == null)
				{
					indexSetCache = type.GetMethod("SetValue", new Type[] { typeof(object), typeof(int) });
					indexSetCachekeyfirst = false;
				}
				var pp = indexSetCache.GetParameters();
				indexSetCachetype1 = pp[0].ParameterType;
				indexSetCachetype2 = pp[1].ParameterType;
			}
			//else
			if (indexSetCachekeyfirst)
			{

				if (key != null && key.GetType() != indexSetCachetype1)
					key = CQuark.AppDomain.GetType(key.GetType()).ConvertTo(content, key, (CQuark.CQType)indexSetCachetype1);
				if (value != null && value.GetType() != indexSetCachetype2)
					value = CQuark.AppDomain.GetType(value.GetType()).ConvertTo(content, value, (CQuark.CQType)indexSetCachetype2);
				indexSetCache.Invoke(object_this, new object[] { key, value });
			}
			else
			{
				if (value != null && value.GetType() != indexSetCachetype1)
					value = CQuark.AppDomain.GetType(value.GetType()).ConvertTo(content, value, (CQuark.CQType)indexSetCachetype1);
				if (key != null && key.GetType() != indexSetCachetype2)
					key = CQuark.AppDomain.GetType(key.GetType()).ConvertTo(content, key, (CQuark.CQType)indexSetCachetype2);

				indexSetCache.Invoke(object_this, new object[] { value, key });
			}
			//var m = type.GetMethods();
			//var targetop = type.GetMethod("set_Item");
			//if (targetop == null)
			//{
			//    //targetop = type.GetMethod("Set");
			//    targetop = type.GetMethod("SetValue", new Type[] { typeof(object), typeof(int) });
			//    targetop.Invoke(object_this, new object[] { value, key });
			//    return;
			//}
			//targetop.Invoke(object_this, new object[] { key, value });
		}
	}
}
