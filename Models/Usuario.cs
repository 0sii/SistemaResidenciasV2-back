using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebApiVinculacionProyectosV2.Models;

public partial class Usuarios
{
    public int Id { get; set; }

    [ValidateNever]
    public string PasswordHash { get; set; } = null!;
    public string Correo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string ApellidoPaterno { get; set; } = null!;
    public string ApellidoMaterno { get; set; } = null!;
    public bool Activo { get; set; }
}
    //