using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Задание.Models;

namespace Задание
{
    public partial class Form1 : Form
    {
        private DbContextOptions<ApplicationContext> options;
        public Form1()
        {
            InitializeComponent();

            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory());
            builder.AddJsonFile("appsetting.json");
            var config = builder.Build();
            string connectionString = config.GetConnectionString("DefaultConnection");
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();
            options = optionsBuilder.UseSqlServer(connectionString).Options;

            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.DataSource = Read(options);
            CalculateStats();
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void MinToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Это моя прога!", "О программе ", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AddtoolStripButton1_Click(object sender, EventArgs e)
        {
            var infoform = new CarsInfoForm();
            infoform.Text = "Добавление Машины";
            if(infoform.ShowDialog(this)==DialogResult.OK)
            {
                Create(options,infoform);
                dataGridView1.DataSource = Read(options);
                CalculateStats();
            }
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dataGridView1.Columns[e.ColumnIndex].Name== "MarkColumn")
            {
                var val = (MarkCars)e.Value;
                switch (val)
                {
                    case (MarkCars.HyundaiCreta):
                        e.Value = "Хендай Крета";
                        break;
                    case (MarkCars.LadaVesta):
                        e.Value = "Лада Веста";
                        break;
                    case (MarkCars.MitsubishiOutlander):
                        e.Value = "Митсубиси Ландер";
                        break;
                }
            }
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            ChangetoolStripButton3.Enabled = DeletetoolStripButton2.Enabled = dataGridView1.SelectedRows.Count > 0;
            ChangeToolStripMenuItem.Enabled = DeleteToolStripMenuItem.Enabled= dataGridView1.SelectedRows.Count > 0;
        }

        private void DeletetoolStripButton2_Click(object sender, EventArgs e)
        {
            var data = (Car)dataGridView1.Rows[dataGridView1.SelectedRows[0].Index].DataBoundItem;
            if (MessageBox.Show($"Вы действительно хотите удалить {data.Mark}  {data.GosNumber} ?", "Удаление Записи", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Delite(options, data);
                dataGridView1.DataSource = Read(options);
                CalculateStats();
            }
        }

        private void ChangetoolStripButton3_Click(object sender, EventArgs e)
        {
            var data = (Car)dataGridView1.Rows[dataGridView1.SelectedRows[0].Index].DataBoundItem;
            var infoform = new CarsInfoForm(data);
            infoform.Text = "Редактирование Автомобиля";
            if (infoform.ShowDialog(this) == DialogResult.OK)
            {
                data.Mark = infoform.Target.Mark;
                data.AvgFuelForHour=infoform.Target.AvgFuelForHour;
                data.Fuel=infoform.Target.Fuel;
                data.PowerReserve = infoform.Target.Fuel / infoform.Target.AvgFuelForHour;
                data.GosNumber=infoform.Target.GosNumber;
                data.Probeg = infoform.Target.Probeg;
                data.PriseRent = infoform.Target.PriseRent;
                data.RentalAmount = infoform.Target.Fuel / infoform.Target.AvgFuelForHour * infoform.Target.PriseRent;
                UpDate(options, data);
                dataGridView1.DataSource = Read(options);
                CalculateStats();
            }
        }
        private void ChangeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangetoolStripButton3.PerformClick();
        }

        private void dataGridView1_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {

            if (e.ColumnIndex >= 0 && e.RowIndex >= 0 && dataGridView1.Columns[e.ColumnIndex].Name == "ProbegColumn")
            {
                e.PaintBackground(e.CellBounds, true);
                var val = decimal.Parse(e.Value.ToString());
                var width = (float)(val/50.0m);

                if (val < 1500)
                {
                    e.Graphics.FillRectangle(Brushes.Green, e.CellBounds.X,
                        e.CellBounds.Y,
                        width,
                        e.CellBounds.Height);
                }
                else if (val < 3000)
                {
                    e.Graphics.FillRectangle(Brushes.Yellow, e.CellBounds.X,
                       e.CellBounds.Y,
                       width,
                       e.CellBounds.Height);
                }
                else 
                {
                    e.Graphics.FillRectangle(Brushes.Red, e.CellBounds.X,
                       e.CellBounds.Y,
                       width,
                       e.CellBounds.Height);
                }
                e.Handled = true;
            }
        }

        public  void CalculateStats()
        {
            var count = Read(options).Count;
            AllCarsStripStatusLabel1.Text = count.ToString();
            var CriticalCount = 0;
            foreach (var car in Read(options))
            {
                if (car.PowerReserve < 7)
                {
                    CriticalCount++;
                }
            }
            CriticalStripStatusLabel1.Text = "Критический запас хода: " + CriticalCount;
        }

        #region MetodsBD
        private static void Create(DbContextOptions<ApplicationContext> options, CarsInfoForm infoform)
        {
            using (ApplicationContext db = new ApplicationContext(options))
            {
                infoform.Target.Id = Guid.NewGuid();
                infoform.Target.PowerReserve = infoform.Target.Fuel / infoform.Target.AvgFuelForHour;
                infoform.Target.RentalAmount = infoform.Target.PowerReserve * infoform.Target.PriseRent;
                db.CarsDB.Add(infoform.Target);
                db.SaveChanges();
            }
        }

        private static void UpDate(DbContextOptions<ApplicationContext> options,Car car)
        {

            using (ApplicationContext db = new ApplicationContext(options))
            {
                var  cars = db.CarsDB.FirstOrDefault(x => x.Id == car.Id);
                if (cars != null)
                {
                    cars.Fuel = car.Fuel;
                    cars.AvgFuelForHour = car.AvgFuelForHour;
                    cars.PowerReserve = car.PowerReserve;
                    cars.Probeg = car.Probeg;
                    cars.GosNumber = car.GosNumber;
                    cars.Mark = car.Mark;
                    cars.RentalAmount = car.RentalAmount;
                    cars.PriseRent = car.PriseRent;
                    db.SaveChanges();
                }
            }
        }

        private static void Delite(DbContextOptions<ApplicationContext> options, Car car)
        {
            using (ApplicationContext db = new ApplicationContext(options))
            {
                var cars = db.CarsDB.FirstOrDefault(x => x.Id ==car.Id );
                if (cars != null)
                {
                    db.CarsDB.Remove(cars);
                    db.SaveChanges();
                }
            }
        }

        private static List<Car> Read(DbContextOptions<ApplicationContext> options)
        {
            using (ApplicationContext db = new ApplicationContext(options))
            {
                return db.CarsDB.ToList();
            }
        }
        #endregion

    }
}