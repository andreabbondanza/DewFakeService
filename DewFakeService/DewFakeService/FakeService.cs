using DewCore.Types.AspNetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
            private Dictionary<int, List<object>> _dataSource = new Dictionary<int, List<object>>();
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
            /// Add a new element to a datasource in service
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="idSource">datasource key</param>
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
            /// <param name="predicate">Filter predicate</param>
            /// <param name="value"></param>
            /// <returns></returns>
            public string Put<T>(int idSource, Func<T, bool> predicate, T value) where T : class, new()
            {
                if (_dataSource.ContainsKey(idSource))
                {
                    _dataSource[idSource].OfType<T>().ToList().ForEach((x) =>
                    {
                        if (predicate(x))
                            x = value;
                    });
                    return new StandardResponse() { Data = new { Text = "Data updated" } }.GetJson();
                }
                return new StandardResponse() { Error = new StandardResponseError("Unable to find datasource") }.GetJson();
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

        }
    }
}
