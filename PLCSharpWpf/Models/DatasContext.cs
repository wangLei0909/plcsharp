using DryIoc.ImTools;
using Microsoft.EntityFrameworkCore;
using PLCSharp.Core.Common;
using PLCSharp.VVMs.Authority;
using PLCSharp.VVMs.Connects;
using PLCSharp.VVMs.GlobalVariables;
using PLCSharp.VVMs.Homepage;
using PLCSharp.VVMs.MotionController;
using PLCSharp.VVMs.Recipe;
using PLCSharp.VVMs.Robots;
using PLCSharp.VVMs.Vision;
using PLCSharp.VVMs.Vision.Camera;
using PLCSharp.VVMs.Workflows;

namespace PLCSharp.Models
{
    /// <summary>
    /// DatasContext
    /// </summary>
    public class DatasContext : DbContext
    {
        /// <summary>
        /// DatasContext
        /// </summary>
        public DatasContext()
        {
            this.Database.EnsureCreated();
            this.EnsureCreatingMissingTables();

        }

        /// <summary>
        /// Users
        /// </summary>
        public DbSet<User> Users { get; set; }
        /// <summary>
        /// Recipes
        /// </summary>
        public DbSet<Recipe> Recipes { get; set; }
        /// <summary>
        /// 系统全局变量集合
        /// </summary>
        public DbSet<SystemVariable> SystemVariables { get; set; }
        /// <summary>
        /// 用户全局变量集合
        /// <summary>
        public DbSet<Variable> Variables { get; set; }
        /// <summary>
        /// Workflows
        /// </summary>
        public DbSet<Workflow> Workflows { get; set; }
        /// <summary>
        /// VisionFunctions
        /// </summary>
        public DbSet<VisionFunction> VisionFunctions { get; set; }
        /// <summary>
        /// 全局图像列表
        /// </summary>
        public DbSet<ImageData> ImageDatas { get; set; }

        /// <summary>
        /// 局部图像列表
        /// </summary>
        public DbSet<LocalImageData> LocalImageDatas { get; set; }
        /// <summary>
        /// 连接模型·
        /// </summary>
        public DbSet<Connect> Connects { get; set; }
        /// <summary>
        /// Cameras
        /// </summary>
        public DbSet<CameraBase> Cameras { get; set; }
        /// <summary>
        /// Controllers
        /// </summary>
        public DbSet<Controller> Controllers { get; set; }
        /// <summary>
        /// Axes
        /// </summary>
        public DbSet<Axis> Axes { get; set; }
        /// <summary>
        /// 轴Points
        /// </summary>
        public DbSet<AxisPoint> AxisPoints { get; set; }

        /// <summary>
        /// 轴Points
        /// </summary>
        public DbSet<Matrix> Matrices { get; set; }

        /// <summary>
        /// 插补Groups
        /// </summary>
        public DbSet<InterpolationGroup> InterpolationGroups { get; set; }
        /// <summary>
        /// DI
        /// </summary>
        public DbSet<DI> DI { get; set; }
        /// <summary>
        /// DQ
        /// </summary>
        public DbSet<DQ> DQ { get; set; }

        /// <summary>
        /// Robots
        /// </summary>
        public DbSet<Robot> Robots { get; set; }
        /// <summary>
        /// 机器人Points
        /// </summary>
        public DbSet<RobotPoint> RobotPoints { get; set; }
        /// <summary>
        /// 机器人矩阵
        /// </summary>
        public DbSet<RobotMatrix> RobotMatrices { get; set; }
        /// <summary>
        /// 错误Logs
        /// </summary>
        public DbSet<ErrorLog> ErrorLogs { get; set; }
        /// <summary>
        /// CustomControls
        /// </summary>
        public DbSet<CustomControl> CustomControls { get; set; }
        /// <summary>
        /// CanvasConfigs
        /// </summary>
        public DbSet<CanvasConfig> CanvasConfigs { get; set; }


        /// <summary>
        /// CurrentRecipe
        /// </summary>
        public Recipe CurrentRecipe { get; set; }
        /// <summary>
        /// OnConfiguring
        /// </summary>
        /// <param name="options">options</param>
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite(@"Data Source=./Config/Datas.db");

        private object _dbLock = new();
        /// <summary>
        /// 保存
        /// </summary>
        public void Save()
        {
            lock (_dbLock)
            {
                SaveChanges();
            }

        }
    }


    /// <summary>
    /// DbContextExtensions
    /// </summary>
    internal static class DbContextExtensions
    {
        /// <summary>
        /// 创建缺失的表
        /// </summary>
        /// <typeparam name="TDbContext"></typeparam>
        /// <param name="dbContext"></param>
        internal static void EnsureCreatingMissingTables<TDbContext>(this TDbContext dbContext) where TDbContext : DbContext
        {
            var type = typeof(TDbContext);
            var dbSetType = typeof(DbSet<>);

            var dbPropertyNames = type.GetProperties().Where(p => p.PropertyType.Name == dbSetType.Name)
                .Select(p => p.Name).ToArray();

            foreach (var entityName in dbPropertyNames)
            {
                CheckTableExistsAndCreateIfMissing(dbContext, entityName);
            }
        }
        private static void CheckTableExistsAndCreateIfMissing(DbContext dbContext, string entityName)
        {
            var defaultSchema = dbContext.Model.GetDefaultSchema();
            var tableName = string.IsNullOrWhiteSpace(defaultSchema) ? $"{entityName}" : $"{defaultSchema}.{entityName}";

            try
            {
                var sqlstr = $"SELECT * FROM {tableName} LIMIT 1";
                _ = dbContext.Database.ExecuteSqlRaw(sqlstr); //Throws on missing table
            }
            catch (Exception)
            {
                var scriptStart = $"CREATE TABLE \"{tableName}\"";

                var script = dbContext.Database.GenerateCreateScript();

                var tableScript = script.Split(scriptStart).Last().Split(";");

                var first = $"{scriptStart} {tableScript.First()}";
                if (first is not null)
                    dbContext.Database.ExecuteSqlRaw(first);

            }
        }
    }
}
