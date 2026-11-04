using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace ToDoList.Models
{
    public class Category
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Color { get; set; } = "#FFFFFF";
    }
}
