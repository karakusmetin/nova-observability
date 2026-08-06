namespace Nova.Observability.Abstractions;

public enum OperationKind
{
    Internal = 0,
    Server = 1,
    Client = 2,
    Producer = 3,
    Consumer = 4
}