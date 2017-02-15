using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace mediaSync
{
    
    public class AppPreferences
    {

        public AppPreferences(){

        }

        internal string _MediaPath;
        public string MediaPath
        {
            get { return _MediaPath; }
            set { _MediaPath = value; }
        }

        internal string _Bucket;
        public string Bucket
        {
            get { return _Bucket; }
            set { _Bucket = value; }
        }

        internal string _AccessKey;
        public string AccessKey
        {
            get { return _AccessKey; }
            set { _AccessKey = value; }
        }

        internal string _SecretKey;
        public string SecretKey
        {
            get { return _SecretKey; }
            set { _SecretKey = value; }
        }

    }
}
