using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogicLayer.Models
{
    public class DocumentFile
    {
        public string FileName { get; set; }
        public  Stream Content { get; set; }
    }
}
