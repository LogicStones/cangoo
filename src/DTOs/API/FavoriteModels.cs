using DTOs.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.API
{
	public class SearchDriversRequest
	{
		public string DriverUserName { get; set; }
	}

	public class SearchDriversResponse : DriversList
	{

	}

	public class AddFavoriteDriverRequest
	{
		public string PassengerId { get; set; }
		public string DriverId { get; set; }
	}

	public class DeleteFavoriteDriverRequest
	{
		public string PassengerId { get; set; }
		public string DriverId { get; set; }
	}

	public class FavoriteDriversListRequest
	{
		public string PassengerId { get; set; }
	}

	public class FavoriteDriversListResponse : DriversList
	{

	}

	public class DriversList
	{
		public string DriverId { get; set; }
		public string Name { get; set; }
		public string Rating { get; set; }
		public string ProfilePicture { get; set; }
	}
}
