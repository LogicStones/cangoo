using AutoMapper;
using DatabaseModel;
using DTOs.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Automapper
{
    public class AutoMapperConfig
    {
        public static IMapper _mapper { get; set; }
        public static void CreateConfiguration()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new AutoMapperProfile());
            });

            _mapper = config.CreateMapper();
        }
    }
}
