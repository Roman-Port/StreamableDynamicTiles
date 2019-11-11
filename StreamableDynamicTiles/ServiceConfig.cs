using System;
using System.Collections.Generic;
using System.Text;

namespace StreamableDynamicTiles
{
    public class ServiceConfig
    {
        public string database_config = "E:\\database_config.json";
        public bool debug_mode = true;
        public int port = 43282;
        public int buffer_size = 8192;
        public int timeout_seconds = 8;

        public string metadata_content_path = "structure_metadata.json";
        public string map_config_file = "map_config.json";
        public string image_content_path = "images.pdip";

        public string builder_ip = "10.0.1.13";
        public int builder_port = 43283;
        public string builder_output = @"C:\Users\Roman\Documents\DWMDynamicTilesTest\";
    }
}
