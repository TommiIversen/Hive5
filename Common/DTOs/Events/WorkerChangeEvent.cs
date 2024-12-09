﻿using Common.DTOs.Enums;

namespace Common.DTOs.Events;

public class WorkerChangeEvent : BaseWorkerInfo
{
    public required ChangeEventType ChangeEventType { get; init; }
}