using Enyim.Caching.Memcached;

namespace API
{
    public interface ICacheD
    {
        bool Store_ADO<T>(string nameSpace, string procedureName, List<ADO_inputParams> inputParams, dynamic data, DateTime expiresAt, string repository = null);
        bool Store_ADO<T>(string nameSpace, string procedureName, List<ADO_inputParams> inputParams, dynamic data, TimeSpan validFor, string repository = null);
        bool Store_BSO<T>(string nameSpace, string className, string methodName, T inputDTO, dynamic data, DateTime expiresAt, string repository = null);
        bool Store_BSO<T>(string nameSpace, string className, string methodName, T inputDTO, dynamic data, TimeSpan validFor, string repository = null);
        bool Store_BSO_REMOVELOCK<T>(string nameSpace, string className, string methodName, T inputDTO, dynamic data, DateTime expiresAt, string repository = null);

        MemCachedD_Value Get_ADO(string nameSpace, string procedureName, List<ADO_inputParams> inputParams);
        MemCachedD_Value Get_BSO<T>(string nameSpace, string className, string methodName, T inputDTO); //
        MemCachedD_Value Get_BSO_WITHLOCK<T>(string nameSpace, string className, string methodName, T inputDTO);
       
        bool Remove_ADO(string nameSpace, string procedureName, List<ADO_inputParams> inputParams);
        bool Remove_BSO<T>(string nameSpace, string className, string methodName, T inputDTO);
        bool CasRepositoryFlush(string repository);
        void FlushAll();
        ServerStats GetStats();
    }
}
