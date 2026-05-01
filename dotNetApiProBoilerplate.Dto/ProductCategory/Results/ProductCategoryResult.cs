using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Dto.ProductCategory.Results
{
    public class ProductCategoryResult
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public int DisplayOrder { get; set; }

        public string? Color { get; set; }

        public string? Icon { get; set; }

    }
}
