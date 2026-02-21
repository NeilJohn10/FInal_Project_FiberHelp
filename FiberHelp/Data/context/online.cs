using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace FiberHelp.Data.context
{
    public class onlineContext : AppDbContext
    {
        public onlineContext(DbContextOptions<onlineContext> options) : base(options)
        {
        }
    }
}
