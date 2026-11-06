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

        [Ignore]
        public string IconSource => Id switch
        {
            1 => "work.png",
            2 => "study.png",
            3 => "house.png",
            4 => "shopping.png",
            5 => "personal.png",
            _ => "alltasks.png"
        };

        [Ignore]
        public string BackgroundColor => "#33" + Color.TrimStart('#');
    }
}
