using TypeGen.Core.TypeAnnotations;

namespace MonEndoVue.Server.Models;

[ExportTsEnum]
public enum TypeTraitement
{
    Medicamenteux = 0,
    NonMedicamenteux = 1
}
