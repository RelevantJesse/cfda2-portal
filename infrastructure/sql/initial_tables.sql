CREATE TABLE Families (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Email NVARCHAR(200) NOT NULL,
    Phone NVARCHAR(50) NOT NULL,
    StripeCustomerId NVARCHAR(200) NULL,
    CreatedUtc DATETIME2 NOT NULL
);

CREATE TABLE Students (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FamilyId INT NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Birthdate DATE NOT NULL,
    Active BIT NOT NULL,
    CONSTRAINT FK_Students_Families FOREIGN KEY (FamilyId) REFERENCES Families(Id)
);

CREATE TABLE Classes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Level NVARCHAR(100) NOT NULL,
    Style NVARCHAR(100) NOT NULL,
    DayOfWeek INT NOT NULL,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    Capacity INT NOT NULL,
    Active BIT NOT NULL
);

CREATE TABLE ClassPricing (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ClassId INT NOT NULL UNIQUE,
    MonthlyTuitionCents INT NOT NULL,
    CONSTRAINT FK_ClassPricing_Classes FOREIGN KEY (ClassId) REFERENCES Classes(Id)
);

CREATE TABLE Enrollments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    StudentId INT NOT NULL,
    ClassId INT NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NULL,
    Status INT NOT NULL,
    CONSTRAINT FK_Enrollments_Students FOREIGN KEY (StudentId) REFERENCES Students(Id),
    CONSTRAINT FK_Enrollments_Classes FOREIGN KEY (ClassId) REFERENCES Classes(Id)
);

CREATE TABLE PaymentMethods (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FamilyId INT NOT NULL,
    Processor INT NOT NULL,
    ProcessorPaymentMethodId NVARCHAR(200) NOT NULL,
    Brand NVARCHAR(50) NOT NULL,
    Last4 NVARCHAR(4) NOT NULL,
    ExpMonth INT NOT NULL,
    ExpYear INT NOT NULL,
    Type INT NOT NULL,
    IsDefault BIT NOT NULL,
    CreatedUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_PaymentMethods_Families FOREIGN KEY (FamilyId) REFERENCES Families(Id)
);

CREATE TABLE AutopaySettings (
    FamilyId INT PRIMARY KEY,
    Enabled BIT NOT NULL,
    DefaultPaymentMethodId INT NULL,
    DraftDay INT NOT NULL,
    GraceDays INT NOT NULL,
    CONSTRAINT FK_AutopaySettings_Families FOREIGN KEY (FamilyId) REFERENCES Families(Id),
    CONSTRAINT FK_AutopaySettings_DefaultPaymentMethod FOREIGN KEY (DefaultPaymentMethodId) REFERENCES PaymentMethods(Id)
);

CREATE TABLE Invoices (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FamilyId INT NOT NULL,
    PeriodStartUtc DATETIME2 NOT NULL,
    PeriodEndUtc DATETIME2 NOT NULL,
    Status INT NOT NULL,
    TotalCents INT NOT NULL,
    CreatedUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_Invoices_Families FOREIGN KEY (FamilyId) REFERENCES Families(Id)
);

CREATE TABLE Charges (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FamilyId INT NOT NULL,
    InvoiceId INT NULL,
    PostedUtc DATETIME2 NOT NULL,
    Kind INT NOT NULL,
    AmountCents INT NOT NULL,
    Memo NVARCHAR(500) NOT NULL,
    CONSTRAINT FK_Charges_Families FOREIGN KEY (FamilyId) REFERENCES Families(Id),
    CONSTRAINT FK_Charges_Invoices FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id)
);

CREATE TABLE Payments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FamilyId INT NOT NULL,
    PostedUtc DATETIME2 NOT NULL,
    Processor INT NOT NULL,
    ProcessorPaymentIntentId NVARCHAR(200) NOT NULL,
    AmountCents INT NOT NULL,
    Memo NVARCHAR(500) NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    CONSTRAINT FK_Payments_Families FOREIGN KEY (FamilyId) REFERENCES Families(Id)
);

CREATE TABLE LedgerEntries (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FamilyId INT NOT NULL,
    InvoiceId INT NULL,
    PostedUtc DATETIME2 NOT NULL,
    Type INT NOT NULL,
    AmountCents INT NOT NULL,
    Memo NVARCHAR(500) NOT NULL,
    CONSTRAINT FK_LedgerEntries_Families FOREIGN KEY (FamilyId) REFERENCES Families(Id),
    CONSTRAINT FK_LedgerEntries_Invoices FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id)
);

CREATE TABLE Discounts (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Kind NVARCHAR(50) NOT NULL,
    ValueCents INT NULL,
    Percent DECIMAL(5,2) NULL,
    CriteriaJson NVARCHAR(MAX) NOT NULL,
    Active BIT NOT NULL
);

CREATE VIEW v_FamilyBalances AS
SELECT f.Id AS FamilyId,
       COALESCE(SUM(CASE WHEN le.Type = 1 THEN le.AmountCents ELSE -le.AmountCents END), 0) AS BalanceCents
FROM Families f
LEFT JOIN LedgerEntries le ON le.FamilyId = f.Id
GROUP BY f.Id;

CREATE VIEW v_AgingBuckets AS
SELECT f.Id AS FamilyId,
       SUM(CASE WHEN DATEDIFF(day, le.PostedUtc, GETUTCDATE()) <= 30 THEN CASE WHEN le.Type = 1 THEN le.AmountCents ELSE -le.AmountCents END ELSE 0 END) AS CurrentCents,
       SUM(CASE WHEN DATEDIFF(day, le.PostedUtc, GETUTCDATE()) > 30 AND DATEDIFF(day, le.PostedUtc, GETUTCDATE()) <= 60 THEN CASE WHEN le.Type = 1 THEN le.AmountCents ELSE -le.AmountCents END ELSE 0 END) AS Over30Cents,
       SUM(CASE WHEN DATEDIFF(day, le.PostedUtc, GETUTCDATE()) > 60 AND DATEDIFF(day, le.PostedUtc, GETUTCDATE()) <= 90 THEN CASE WHEN le.Type = 1 THEN le.AmountCents ELSE -le.AmountCents END ELSE 0 END) AS Over60Cents,
       SUM(CASE WHEN DATEDIFF(day, le.PostedUtc, GETUTCDATE()) > 90 THEN CASE WHEN le.Type = 1 THEN le.AmountCents ELSE -le.AmountCents END ELSE 0 END) AS Over90Cents
FROM Families f
LEFT JOIN LedgerEntries le ON le.FamilyId = f.Id
GROUP BY f.Id;