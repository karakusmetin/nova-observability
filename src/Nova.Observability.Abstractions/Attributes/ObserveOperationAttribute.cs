using System;

namespace Nova.Observability.Abstractions;

/// <summary>
/// Bir metodun Nova tarafından business operation
/// olarak gözlemlenmesini tanımlar.
///
/// Attribute yalnızca metadata taşır.
/// Gerçek instrumentation işlemi interceptor
/// tarafından gerçekleştirilir.
/// </summary>
[AttributeUsage( AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ObserveOperationAttribute : Attribute
{
    /// <summary>
    /// Teknik ve sabit operation adı.
    ///
    /// Örnek:
    /// kep.detail.process
    /// document.convert
    /// esign.finalize
    /// </summary>
    public ObserveOperationAttribute(
        string name)
    {
        Name = name;
    }

    /// <summary>
    /// Metric ve trace sorgularında kullanılacak
    /// teknik operation adı.
    ///
    /// Yüksek cardinality oluşturan dinamik
    /// değerler burada kullanılmamalıdır.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Monitoring ekranında kullanıcıya
    /// gösterilecek okunabilir operation adı.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Operation'ın trace içerisindeki türü.
    /// </summary>
    public OperationKind Kind { get; set; } =
        OperationKind.Internal;

    /// <summary>
    /// Business domain.
    ///
    /// Örnek:
    /// KEP
    /// Document
    /// Workflow
    /// ElectronicSignature
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Business action.
    ///
    /// Örnek:
    /// Process
    /// Convert
    /// Validate
    /// Publish
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// İşlenen business entity tipi.
    ///
    /// Örnek:
    /// KEPGelen
    /// Dokuman
    /// TaslakBelge
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// EntityId olarak kullanılacak
    /// metod parametresinin adı.
    ///
    /// Örnek:
    /// EntityIdParameter = "kepGelenId"
    /// </summary>
    public string? EntityIdParameter { get; set; }

    /// <summary>
    /// CorrelationId olarak kullanılacak
    /// metod parametresinin adı.
    ///
    /// Belirtilmezse Nova mevcut Activity
    /// context'ini kullanacaktır.
    /// </summary>
    public string? CorrelationIdParameter { get; set; }
}