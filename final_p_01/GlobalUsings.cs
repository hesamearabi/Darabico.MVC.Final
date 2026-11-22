// Restores the namespaces broken by the bfad288 rename of ApplicationDbContext
// (Infrastructure.Data -> Infrastructure) and the missing helper using in the
// admin ProjectsController, without editing every consuming file.
global using final_p_01.Infrastructure.Data;
global using Darabico.Web.Helpers;
