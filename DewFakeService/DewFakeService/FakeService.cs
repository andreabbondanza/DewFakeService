using DewCore.Types.AspNetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DewCore.Extensions.Strings;
using JWT;
using System.Text;
using JWT.Serializers;
using JWT.Algorithms;

namespace DewCore.Types.Complex
{
    namespace Development
    {
        /// <summary>
        /// Json producer attribute
        /// </summary>
        [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
        public sealed class JsonProductAttribute : Attribute
        {

        }
        /// <summary>
        /// Custom produces attribute
        /// </summary>
        [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
        public sealed class CustomProductAttribute : Attribute
        {

        }
        /// <summary>
        /// Xml produces attribute
        /// </summary>
        [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
        public sealed class XmlProductAttribute : Attribute
        {

        }
        [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
        public sealed class NoUpdateAttribute : Attribute
        {

        }
        /// <summary>
        /// A fake service for test/development
        /// </summary>
        public class FakeService
        {
            private enum Produces
            {
                Json,
                Xml,
                Custom
            }
            private static Dictionary<int, List<object>> _dataSource = new Dictionary<int, List<object>>();
            /// <summary>
            /// Token secret key
            /// </summary>
            public string Secret = "carriage";
            private Produces CheckProductionType(MethodBase t)
            {
                var attrs = t.GetCustomAttributes(false);
                if (attrs.FirstOrDefault(x => x.GetType() == typeof(JsonProductAttribute)) != default(Attribute))
                    return Produces.Json;
                if (attrs.FirstOrDefault(x => x.GetType() == typeof(CustomProductAttribute)) != default(Attribute))
                    return Produces.Custom;
                if (attrs.FirstOrDefault(x => x.GetType() == typeof(XmlProductAttribute)) != default(Attribute))
                    return Produces.Xml;
                return Produces.Json;
            }
            private string SerializeXml<T>(int idSource, Func<T, bool> predicate = null) where T : class, new()
            {
                throw new NotImplementedException();
            }
            private string SerializeJson<T>(int idSource, Func<T, bool> predicate = null) where T : class, new()
            {
                if (predicate == null)
                    predicate = (x) => true;
                List<T> ds = new List<T>();
                if (_dataSource.ContainsKey(idSource))
                    ds = _dataSource[idSource].OfType<T>().Where(predicate).ToList();
                var response = new StandardResponse<IEnumerable<T>>() { Data = ds };
                return response.GetJson();
            }
            private string SerializeJson<T>(int idSource, int offset, int limit, Func<T, bool> predicate = null) where T : class, new()
            {
                if (predicate == null)
                    predicate = (x) => true;
                List<T> ds = new List<T>();
                if (_dataSource.ContainsKey(idSource))
                    ds = _dataSource[idSource].OfType<T>().Where(predicate).Skip(offset).Take(limit).ToList();
                var response = new StandardResponse<IEnumerable<T>>() { Data = ds };
                return response.GetJson();
            }
            private bool TokenValidation(string token)
            {
                try
                {
                    IJsonSerializer serializer = new JsonNetSerializer();
                    IDateTimeProvider provider = new UtcDateTimeProvider();
                    IJwtValidator validator = new JwtValidator(serializer, provider);
                    IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
                    IJwtDecoder decoder = new JwtDecoder(serializer, validator, urlEncoder);

                    var json = decoder.Decode(token, Secret, verify: true);
                    return true;
                }
                catch (TokenExpiredException)
                {
                    return false;
                }
                catch (SignatureVerificationException)
                {
                    return false;
                }
            }
            /// <summary>
            /// Add a new datasource to the service
            /// </summary>
            /// <returns>the identification key of datasource</returns>
            public int AddDataSource()
            {
                var key = _dataSource.Count + 1;
                _dataSource.Add(key, new List<object>());
                return key;
            }
            /// <summary>
            /// Add and load a new datasource to the service
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="dataSource"></param>
            /// <returns>the id key of datasource</returns>
            public int AddDataSource<T>(List<T> dataSource) where T : class, new()
            {
                var key = _dataSource.Count + 1;
                _dataSource.Add(key, dataSource.ToList<object>());
                return key;
            }
            /// <summary>
            /// Generate data in a new datasource
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="prefix"></param>
            /// <param name="quantity"></param>
            /// <returns></returns>
            public int AddDataSourceGenerator<T>(int quantity) where T : class, new()
            {
                var key = _dataSource.Count + 1;
                var subject = typeof(T);
                var rand = new Random();
                var list = new List<T>();
                for (int i = 0; i < quantity; i++)
                {
                    var props = subject.GetRuntimeProperties();
                    var temp = new T();
                    foreach (var item in props)
                    {
                        if (item.PropertyType == typeof(int) || item.PropertyType == typeof(long) || item.PropertyType == typeof(double)
                            || item.PropertyType == typeof(float) || item.PropertyType == typeof(byte) || item.PropertyType == typeof(sbyte))
                        {
                            item.SetValue(temp, i);
                        }
                        if (item.PropertyType == typeof(DateTime))
                            item.SetValue(temp, DateTime.Now.AddDays(i));
                        if (item.PropertyType == typeof(TimeSpan))
                            item.SetValue(temp, DateTime.Now.AddMinutes(i).TimeOfDay);
                        if (item.PropertyType == typeof(string))
                            item.SetValue(temp, item.Name + "_" + i);
                        if (item.PropertyType == typeof(bool))
                            item.SetValue(temp, rand.Next(-10, 10) > 0);
                        var x = item.PropertyType.GetConstructor(Type.EmptyTypes);
                        if (item.PropertyType.GetConstructor(Type.EmptyTypes) != default(ConstructorInfo))
                        {
                            if (!item.PropertyType.IsGenericType)
                            {
                                item.SetValue(temp, Activator.CreateInstance(item.PropertyType));
                            }
                        }
                    }
                    list.Add(temp);
                }
                _dataSource.Add(key, list.ToList<object>());
                return key;
            }
            /// <summary>
            /// Clear a datasource in the service
            /// </summary>
            /// <param name="idSource"></param>
            /// <returns></returns>
            public bool ClearDataSource(int idSource)
            {
                if (_dataSource.ContainsKey(idSource))
                {
                    _dataSource[idSource].Clear();
                    return true;
                }
                return false;
            }
            /// <summary>
            /// Get all elements of the given key datasource, default is json (change with attributes)
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="idSource"></param>
            /// <param name="customProducer"></param>
            /// <returns></returns>
            public string Get<T>(int idSource, Func<IEnumerable<T>, string> customProducer = null) where T : class, new()
            {
                var productType = CheckProductionType(MethodBase.GetCurrentMethod());
                switch (productType)
                {
                    case Produces.Json:
                        return SerializeJson<T>(idSource);
                    case Produces.Xml:
                        return SerializeXml<T>(idSource);
                    case Produces.Custom:
                        return customProducer != null ? customProducer(_dataSource[idSource].OfType<T>().ToList()) : null;
                }
                throw new Exception("Something goes wrong");
            }
            /// <summary>
            /// Get filtred elements of the given key datasource, default is json
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="idSource"></param>
            /// <param name="predicate"></param>
            /// <param name="customProducer"></param>
            /// <returns></returns>
            public string Get<T>(int idSource, Func<T, bool> predicate, Func<IEnumerable<T>, string> customProducer = null) where T : class, new()
            {
                try
                {
                    var productType = CheckProductionType(MethodBase.GetCurrentMethod());
                    switch (productType)
                    {
                        case Produces.Json:
                            return SerializeJson<T>(idSource, predicate);
                        case Produces.Xml:
                            return SerializeXml<T>(idSource);
                        case Produces.Custom:
                            return customProducer != null ? customProducer(_dataSource[idSource].OfType<T>().Where(predicate).ToList()) : null;
                    }
                }
                catch
                {

                }
                return new StandardResponse() { Error = new StandardResponseError("No method recognized") }.GetJson();
            }
            /// <summary>
            /// Get all elements of the given key datasource, default is json (change with attributes)
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="idSource"></param>
            /// <param name="token"></param>
            /// <param name="customProducer"></param>
            /// <returns></returns>
            public string Get<T>(int idSource, string token, Func<IEnumerable<T>, string> customProducer = null) where T : class, new()
            {
                if (!TokenValidation(token))
                    return new StandardResponse() { Error = new StandardResponseError("Unauthorized access") }.GetJson();
                var productType = CheckProductionType(MethodBase.GetCurrentMethod());
                switch (productType)
                {
                    case Produces.Json:
                        return SerializeJson<T>(idSource);
                    case Produces.Xml:
                        return SerializeXml<T>(idSource);
                    case Produces.Custom:
                        return customProducer != null ? customProducer(_dataSource[idSource].OfType<T>().ToList()) : null;
                }
                throw new Exception("Something goes wrong");
            }
            /// <summary>
            /// Get filtred elements of the given key datasource, default is json
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="idSource"></param>
            /// <param name="token"></param>
            /// <param name="predicate"></param>
            /// <param name="customProducer"></param>
            /// <returns></returns>
            public string Get<T>(int idSource, string token, Func<T, bool> predicate, Func<IEnumerable<T>, string> customProducer = null) where T : class, new()
            {
                if (!TokenValidation(token))
                    return new StandardResponse() { Error = new StandardResponseError("Unauthorized access") }.GetJson();
                try
                {
                    var productType = CheckProductionType(MethodBase.GetCurrentMethod());
                    switch (productType)
                    {
                        case Produces.Json:
                            return SerializeJson<T>(idSource, predicate);
                        case Produces.Xml:
                            return SerializeXml<T>(idSource);
                        case Produces.Custom:
                            return customProducer != null ? customProducer(_dataSource[idSource].OfType<T>().Where(predicate).ToList()) : null;
                    }
                }
                catch
                {

                }
                return new StandardResponse() { Error = new StandardResponseError("No method recognized") }.GetJson();
            }
            /// <summary>
            /// Get all elements of the given key datasource, default is json (change with attributes)
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="idSource"></param>
            /// <param name="customProducer"></param>
            /// <param name="offset"></param>
            /// <param name="limit"></param>
            /// <returns></returns>
            public string Get<T>(int idSource, int offset, int limit, Func<IEnumerable<T>, string> customProducer = null) where T : class, new()
            {
                var productType = CheckProductionType(MethodBase.GetCurrentMethod());
                switch (productType)
                {
                    case Produces.Json:
                        return SerializeJson<T>(idSource, offset, limit);
                    case Produces.Xml:
                        return SerializeXml<T>(idSource);
                    case Produces.Custom:
                        return customProducer != null ? customProducer(_dataSource[idSource].OfType<T>().Skip(offset).Take(10).ToList()) : null;
                }
                throw new Exception("Something goes wrong");
            }
            /// <summary>
            /// Get filtred elements of the given key datasource, default is json
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="idSource"></param>
            /// <param name="predicate"></param>
            /// <param name="customProducer"></param>
            /// <param name="offset"></param>
            /// /// <param name="limit"></param>
            /// <returns></returns>
            public string Get<T>(int idSource, int offset, int limit, Func<T, bool> predicate, Func<IEnumerable<T>, string> customProducer = null) where T : class, new()
            {
                try
                {
                    var productType = CheckProductionType(MethodBase.GetCurrentMethod());
                    switch (productType)
                    {
                        case Produces.Json:
                            return SerializeJson<T>(idSource, offset, limit, predicate);
                        case Produces.Xml:
                            return SerializeXml<T>(idSource);
                        case Produces.Custom:
                            return customProducer != null ? customProducer(_dataSource[idSource].OfType<T>().Where(predicate).Skip(offset).Take(10).ToList()) : null;
                    }
                }
                catch
                {

                }
                return new StandardResponse() { Error = new StandardResponseError("No method recognized") }.GetJson();
            }
            /// <summary>
            /// Get all elements of the given key datasource, default is json (change with attributes)
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="idSource"></param>
            /// <param name="token"></param>
            /// <param name="customProducer"></param>
            /// <param name="offset"></param>
            /// <returns></returns>
            public string Get<T>(int idSource, string token, int offset, int limit, Func<IEnumerable<T>, string> customProducer = null) where T : class, new()
            {
                if (!TokenValidation(token))
                    return new StandardResponse() { Error = new StandardResponseError("Unauthorized access") }.GetJson();
                var productType = CheckProductionType(MethodBase.GetCurrentMethod());
                switch (productType)
                {
                    case Produces.Json:
                        return SerializeJson<T>(idSource, offset, limit);
                    case Produces.Xml:
                        return SerializeXml<T>(idSource);
                    case Produces.Custom:
                        return customProducer != null ? customProducer(_dataSource[idSource].OfType<T>().Skip(offset).Take(10).ToList()) : null;
                }
                throw new Exception("Something goes wrong");
            }
            /// <summary>
            /// Get filtred elements of the given key datasource, default is json
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="idSource"></param>
            /// <param name="token"></param>
            /// <param name="predicate"></param>
            /// <param name="offset"></param>
            /// <param name="customProducer"></param>
            /// <returns></returns>
            public string Get<T>(int idSource, int offset, int limit,
                string token, Func<T, bool> predicate, Func<IEnumerable<T>, string> customProducer = null) where T : class, new()
            {
                if (!TokenValidation(token))
                    return new StandardResponse() { Error = new StandardResponseError("Unauthorized access") }.GetJson();
                try
                {
                    var productType = CheckProductionType(MethodBase.GetCurrentMethod());
                    switch (productType)
                    {
                        case Produces.Json:
                            return SerializeJson<T>(idSource, offset, limit, predicate);
                        case Produces.Xml:
                            return SerializeXml<T>(idSource);
                        case Produces.Custom:
                            return customProducer != null ? customProducer(_dataSource[idSource].OfType<T>().Where(predicate).Skip(offset).Take(10).ToList()) : null;
                    }
                }
                catch
                {

                }
                return new StandardResponse() { Error = new StandardResponseError("No method recognized") }.GetJson();
            }
            /// <summary>
            /// Add a new element to a datasource in service
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="idSource">datasource key</param>
            /// <param name="token"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public string Post<T>(int idSource, string token, T value) where T : class, new()
            {
                if (!TokenValidation(token))
                    return new StandardResponse() { Error = new StandardResponseError("Unauthorized access") }.GetJson();
                if (_dataSource.ContainsKey(idSource))
                {
                    _dataSource[idSource].Add(value);
                    return new StandardResponse() { Data = new { Text = "Data inserted" } }.GetJson();
                }
                return new StandardResponse() { Error = new StandardResponseError("Unable to find datasource") }.GetJson();
            }
            /// <summary>
            /// Add a new element to a datasource in service
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="idSource">datasource key</param>
            /// <param name="token"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public string Post<T>(int idSource, T value) where T : class, new()
            {
                if (_dataSource.ContainsKey(idSource))
                {
                    _dataSource[idSource].Add(value);
                    return new StandardResponse() { Data = new { Text = "Data inserted" } }.GetJson();
                }
                return new StandardResponse() { Error = new StandardResponseError("Unable to find datasource") }.GetJson();
            }
            /// <summary>
            /// Update an element in a datasource in service
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="idSource">datasource key</param>
            /// <param name="token"></param>
            /// <param name="predicate">Filter predicate</param>
            /// <param name="value"></param>
            /// <returns></returns>
            public string Put<T>(int idSource, string token, Func<T, bool> predicate, T value) where T : class, new()
            {
                if (!TokenValidation(token))
                    return new StandardResponse() { Error = new StandardResponseError("Unauthorized access") }.GetJson();
                if (_dataSource.ContainsKey(idSource))
                {
                    var l = _dataSource[idSource].OfType<T>().ToList();
                    foreach (var item in l)
                    {
                        if (predicate(item))
                        {
                            var prop = item.GetType().GetRuntimeProperties();
                            foreach (var item1 in prop)
                            {
                                var attr = item1.GetCustomAttributes().FirstOrDefault(x => x.GetType() == typeof(NoUpdateAttribute));
                                if (attr == (default(Attribute)))
                                {
                                    var temp = value.GetType().GetRuntimeProperty(item1.Name);
                                    item1.SetValue(item, temp.GetValue(value));
                                }
                            }
                            break;
                        }
                    }
                    return new StandardResponse() { Data = new { Text = "Data updated" } }.GetJson();
                }
                return new StandardResponse() { Error = new StandardResponseError("Unable to find datasource") }.GetJson();
            }
            /// <summary>
            /// Update an element in a datasource in service
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="idSource">datasource key</param>
            /// <param name="token"></param>
            /// <param name="predicate">Filter predicate</param>
            /// <param name="value"></param>
            /// <returns></returns>
            public string Put<T>(int idSource, Func<T, bool> predicate, T value) where T : class, new()
            {
                if (_dataSource.ContainsKey(idSource))
                {
                    var l = _dataSource[idSource].OfType<T>().ToList();
                    foreach (var item in l)
                    {
                        if (predicate(item))
                        {
                            var prop = item.GetType().GetRuntimeProperties();
                            foreach (var item1 in prop)
                            {
                                var attr = item1.GetCustomAttributes().FirstOrDefault(x => x.GetType() == typeof(NoUpdateAttribute));
                                if (attr == (default(Attribute)))
                                {
                                    var temp = value.GetType().GetRuntimeProperty(item1.Name);
                                    item1.SetValue(item, temp.GetValue(value));
                                }
                            }
                            break;
                        }
                    }
                    return new StandardResponse() { Data = new { Text = "Data updated" } }.GetJson();
                }
                return new StandardResponse() { Error = new StandardResponseError("Unable to find datasource") }.GetJson();
            }
            /// <summary>
            /// Return a jwt token of an object
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="value"></param>
            /// <param name="secret"></param>
            /// <returns></returns>
            public string Login<T>(T value, string secret = null)
            {
                secret = secret ?? Secret;
                IJwtAlgorithm algorithm = new HMACSHA256Algorithm();
                IJsonSerializer serializer = new JsonNetSerializer();
                IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
                IJwtEncoder encoder = new JwtEncoder(algorithm, serializer, urlEncoder);
                var token = encoder.Encode(value, secret);
                return token;
            }
            /// <summary>
            /// Encode base 64 string
            /// </summary>
            /// <param name="toEncode"></param>
            /// <returns></returns>
            public static string Base64Encode(string toEncode)
            {
                var b64 = new JwtBase64UrlEncoder();
                return b64.Encode(toEncode.ToBytes());
            }
            /// <summary>
            /// Decode a base 64 string
            /// </summary>
            /// <param name="toDecode"></param>
            /// <returns></returns>
            public static byte[] Base64Decode(string toDecode)
            {
                var b64 = new JwtBase64UrlEncoder();
                return b64.Decode(toDecode);
            }
        }
    }
}
