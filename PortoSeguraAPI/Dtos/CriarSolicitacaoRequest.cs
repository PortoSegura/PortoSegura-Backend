public class CriarSolicitacaoRequest
{
    public required int UsuariaId { get; set; }
    public required int MadrinhaId { get; set; }
    public required string Descricao { get; set; }
    public required DateTime DataInicio { get; set; }
    public required DateTime DataFim { get; set; }
    public required int QtdDiarias { get; set; }
    public required decimal Valor { get; set; }
}