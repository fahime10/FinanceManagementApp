CREATE TABLE users (
	user_id INT NOT NULL PRIMARY KEY IDENTITY,
	user_first_name VARCHAR (20) NOT NULL,
	user_last_name VARCHAR (20) NOT NULL,
	user_email_address VARCHAR (50) NOT NULL UNIQUE,
	user_password VARCHAR (256) NOT NULL
);

CREATE TABLE incomes (
	income_id INT NOT NULL PRIMARY KEY IDENTITY,
	user_id INT NOT NULL,
	income_description VARCHAR (100) NOT NULL,
	income_amount FLOAT NOT NULL,
	transaction_date DATETIME NOT NULL,
	CONSTRAINT income_user FOREIGN KEY (user_id)
		REFERENCES users(user_id)
		ON DELETE CASCADE
		ON UPDATE CASCADE
);

CREATE TABLE expenses (
	expense_id INT NOT NULL PRIMARY KEY IDENTITY,
	user_id INT NOT NULL,
	expense_description VARCHAR (100) NOT NULL,
	expense_amount FLOAT NOT NULL,
	transaction_date DATETIME NOT NULL,
	CONSTRAINT expense_user FOREIGN KEY (user_id)
		REFERENCES users(user_id)
		ON DELETE CASCADE
		ON UPDATE CASCADE
);

CREATE TABLE budgets (
	budget_id INT NOT NULL PRIMARY KEY IDENTITY,
	user_id INT NOT NULL,
	budget_amount FLOAT NOT NULL,
	budget_created_at DATETIME NOT NULL,
	CONSTRAINT budget_user FOREIGN KEY (user_id)
		REFERENCES users(user_id)
		ON DELETE CASCADE
		ON UPDATE CASCADE
);

CREATE TABLE notifications (
	notification_id INT NOT NULL PRIMARY KEY IDENTITY,
	message VARCHAR (256) NOT NULL,
	user_id INT NOT NULL,
	created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
	is_read BOOLEAN DEFAULT FALSE,
	CONSTRAINT notification_user FOREIGN KEY (user_id)
		REFERENCES users(user_id)
		ON DELETE CASCADE
		ON UPDATE CASCADE
); 
