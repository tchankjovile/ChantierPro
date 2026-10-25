using ChantierPro.Core.Models;

namespace ChantierPro.Api.Dtos;

public record CreateChantierRequest(
    string Reference,
    string Name,
    string Client,
    string Address,
    double? Latitude,
    double? Longitude,
    DateTime StartDate,
    DateTime PlannedEndDate,
    decimal TotalBudget,
    ChantierStatus Status,
    string Description,
    bool GenerateDefaultPhases = true);

public record UpdateChantierRequest(
    string Reference,
    string Name,
    string Client,
    string Address,
    double? Latitude,
    double? Longitude,
    DateTime StartDate,
    DateTime PlannedEndDate,
    decimal TotalBudget,
    ChantierStatus Status,
    string Description);

public record SetStatusRequest(ChantierStatus Status);

public record SavePhaseRequest(
    decimal Progress,
    string? Note);

public record CreatePhaseRequest(
    string Name,
    int Order,
    decimal Weight,
    DateTime StartDate,
    DateTime EndDate,
    decimal AllocatedBudget);

public record UpdatePhaseDetailsRequest(
    string Name,
    int Order,
    decimal Weight,
    DateTime StartDate,
    DateTime EndDate,
    decimal AllocatedBudget);

public record CreateTaskRequest(string Label, int Order);
public record UpdateTaskRequest(string Label, bool IsDone, int Order);

public record CreateMovementRequest(
    Guid ChantierId,
    Guid MaterialId,
    MovementType Type,
    decimal Quantity,
    decimal UnitPrice,
    Guid? PhaseId,
    Guid? SupplierId,
    string? DeliveryNoteNumber,
    DateTime? Date,
    bool IsNegativeStockAuthorized);

public record CreateExpenseRequest(
    Guid ChantierId,
    BudgetCategory Category,
    decimal Amount,
    DateTime Date,
    string Supplier,
    string Label,
    PaymentMode PaymentMode,
    string? ReceiptPhotoPath);

public record SaveAttendancesRequest(
    DateTime Date,
    List<AttendanceItemDto> Attendances);

public record AttendanceItemDto(
    Guid WorkerId,
    Guid? PhaseId,
    AttendanceStatus Status,
    decimal OvertimeHours);

public record CreateJournalEntryRequest(
    DateTime Date,
    WeatherKind Weather,
    string Note,
    string Incident,
    string Visitors);

public record CreatePhotoRequest(
    string Path,
    string Stage);
