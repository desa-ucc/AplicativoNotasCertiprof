CREATE TABLE cert_registros (
    id INT IDENTITY(1,1) PRIMARY KEY,
    email NVARCHAR(255),
    first_name NVARCHAR(255),
    last_name NVARCHAR(255),
    certification_name NVARCHAR(255) NOT NULL,
    status NVARCHAR(100),
    percentage DECIMAL(5,2),
    cedula NVARCHAR(50),
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    upload_history_id INT NOT NULL
);
