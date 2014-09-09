using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace KuaFu.NetEase
{
    public class StockInfo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int StockInfoId { get; set; }

        [Index(IsUnique = true)]
        [MaxLength(50)]
        public string Code { get; set; }

        [Index(IsUnique = true)]
        [MaxLength(50)]
        public string Symbol { get; set; }

        public string Name { get; set; }
    }
}
