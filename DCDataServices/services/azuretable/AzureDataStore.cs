using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCDataServices.services.azuretable
{
    public class AzureDataStore
    {
        private static readonly string DB_NAME = "azure_data_store.db";
        private static LiteDatabase liteDB;
        private static int maxElementCount;
        private static ILiteCollection<NodeDataDB> nd_collection = null;

        private static void InitLiteDB()
        {
            if (liteDB == null)
            {
                try
                {
                    string db_dir = AppOptions.GetAppOptions.DBDirectory;
                    maxElementCount = AppOptions.GetAppOptions.MaximumStoredSamples;
                    // open database (or create if doesn't exist)
                    string db_path = System.IO.Path.Combine(db_dir, DB_NAME);
                    liteDB = new LiteDatabase(db_path);
                    nd_collection = liteDB.GetCollection<NodeDataDB>("nodedata");

                }
                catch (Exception e_initdb)
                {
                    LogUtils.DCLogger.Error(e_initdb, "InitLiteDB()");
                    liteDB = null;
                }            
            }
        }

        public static int GetNumberOfElements()
        {
            if (liteDB == null)
            {
                InitLiteDB();
            }
            if (liteDB == null)
            {
                return 0;
            }
            else
            {
                List<NodeDataDB> tl_nd = nd_collection.Query()
                    .OrderBy(x => x.SampleTime)
                    .ToList();
                return nd_collection.Count();
            }
        }


        public static List<NodeDataDB> GetElements(int count)
        {
            // if count == -1, return all avaialble elements
            if (liteDB == null)
            {
                // attempt to init
                InitLiteDB();
            }
            // failed initialization
            if (liteDB == null)
            {
                return null;
            }
            List<NodeDataDB> results = null;
            if (nd_collection != null)
            {
                if (count == -1)
                {
                    // return all elements
                    results = nd_collection.Query()
                        .OrderBy(x => x.SampleTime)
                        .ToList();
                }
                else
                {
                    // return count elements
                    results = nd_collection.Query()
                        .OrderBy(x => x.SampleTime)
                        .Limit(count)
                        .ToList();
                }
            }
            return results;
        }

        public static int DeleteAll()
        {
            if (nd_collection == null)
            {
                return 0;
            }
            // sampletime monotonically increases, so it deletes all elements whose SAmpleTime < upto_element.SampleTime
            int deleted_count = nd_collection.DeleteAll();
            return deleted_count;
        }

        /// <summary>
        /// deletes elements in db whose SampleTime is less than or equal to upto_element.SampleTime
        /// </summary>
        /// <param name="upto_element" type="NodeDataDB"></param>
        /// <returns>
        /// number of elements deleted
        /// </returns>
        public static int DeleteElements(NodeDataDB upto_element)
        {
            if (nd_collection == null)
            {
                return 0;
            }
            // sampletime monotonically increases, so it deletes all elements whose SAmpleTime < upto_element.SampleTime
            int deleted_count = nd_collection.DeleteMany(x => x.SampleTime <= upto_element.SampleTime);
            return deleted_count;

        }

        public static int DeleteElements(int count_of_elements)
        {
            List<NodeDataDB> elements = nd_collection.Query()
                .OrderBy(x => x.SampleTime)
                .Limit(count_of_elements)
                .ToList();
            NodeDataDB last_element = elements.Last();
            int num_del = DeleteElements(last_element);
            return num_del;
        }

        public static bool InsertElement(NodeDataDB nd_db)
        {
            if (nd_collection == null)
            {
                InitLiteDB();
            }
            if (nd_collection != null)
            {
                if (nd_collection.Count() > maxElementCount)
                {
                    int to_remove_count = nd_collection.Count() - maxElementCount;
                    List<NodeDataDB> elements = nd_collection.Query()
                        .OrderBy(x => x.SampleTime)
                        .Limit(to_remove_count)
                        .ToList();
                    NodeDataDB last_element = elements.Last();
                    DeleteElements(last_element);
                }
                nd_collection.Insert(nd_db);
                return true;
            }
            else
            {
                LogUtils.DCLogger.Error("LiteDB init failed. Discarding elements.");
                LogUtils.DCLogger.Error(nd_db.NDJson);
                return false;
            }
        }
    }
}
