/**
 * sql.js Interop Layer for Blazor WebAssembly
 *
 * Provides a bridge between Blazor C# code and sql.js (SQLite compiled to WASM).
 * Manages multiple database instances with UUID-based identification.
 *
 * Usage:
 *   await SqlJsInterop.init();
 *   const dbId = await SqlJsInterop.openDb(uint8Array);
 *   const rows = await SqlJsInterop.exec(dbId, "SELECT * FROM table");
 *   await SqlJsInterop.close(dbId);
 */
window.SqlJsInterop = {
    _SQL: null,
    _dbs: {},

    /**
     * Initialize sql.js library.
     * Must be called once before any database operations.
     */
    init: async function () {
        if (this._SQL) return true;

        try {
            // Load sql.js with the WASM binary
            this._SQL = await initSqlJs({
                locateFile: file => `/sqljs/${file}`
            });
            console.log('[SqlJs] Initialized successfully');
            return true;
        } catch (error) {
            console.error('[SqlJs] Initialization failed:', error);
            throw error;
        }
    },

    /**
     * Open a database from bytes.
     * @param {Uint8Array|null} bytes - Database file bytes, or null for empty database
     * @returns {string} Database ID for future operations
     */
    openDb: function (bytes) {
        if (!this._SQL) {
            throw new Error('sql.js not initialized. Call init() first.');
        }

        const id = this._generateId();
        try {
            const db = bytes && bytes.length > 0
                ? new this._SQL.Database(bytes)
                : new this._SQL.Database();
            this._dbs[id] = db;
            console.log(`[SqlJs] Opened database: ${id}`);
            return id;
        } catch (error) {
            console.error('[SqlJs] Failed to open database:', error);
            throw error;
        }
    },

    /**
     * Execute a SQL query and return results.
     * @param {string} id - Database ID
     * @param {string} sql - SQL query to execute
     * @param {Array|null} params - Optional query parameters
     * @returns {Array} Array of row objects
     */
    exec: function (id, sql, params) {
        const db = this._dbs[id];
        if (!db) {
            throw new Error(`Database not found: ${id}`);
        }

        try {
            const results = db.exec(sql, params || []);
            if (results.length === 0) {
                return [];
            }

            // Convert to array of objects with column names as keys
            const { columns, values } = results[0];
            return values.map(row => {
                const obj = {};
                columns.forEach((col, i) => {
                    obj[col] = row[i];
                });
                return obj;
            });
        } catch (error) {
            console.error('[SqlJs] Query failed:', error);
            throw error;
        }
    },

    /**
     * Execute a SQL statement that doesn't return results (INSERT, UPDATE, DELETE, CREATE).
     * @param {string} id - Database ID
     * @param {string} sql - SQL statement to execute
     * @param {Array|null} params - Optional query parameters
     * @returns {number} Number of rows affected
     */
    run: function (id, sql, params) {
        const db = this._dbs[id];
        if (!db) {
            throw new Error(`Database not found: ${id}`);
        }

        try {
            db.run(sql, params || []);
            return db.getRowsModified();
        } catch (error) {
            console.error('[SqlJs] Statement failed:', error);
            throw error;
        }
    },

    /**
     * Get table schema information.
     * @param {string} id - Database ID
     * @returns {Array} Array of table info objects
     */
    getTables: function (id) {
        return this.exec(id,
            "SELECT name, type FROM sqlite_master WHERE type IN ('table', 'view') AND name NOT LIKE 'sqlite_%' ORDER BY name");
    },

    /**
     * Get column information for a table.
     * @param {string} id - Database ID
     * @param {string} tableName - Table name
     * @returns {Array} Array of column info objects
     */
    getColumns: function (id, tableName) {
        return this.exec(id, `PRAGMA table_info('${tableName}')`);
    },

    /**
     * Export database as byte array.
     * @param {string} id - Database ID
     * @returns {Uint8Array} Database bytes
     */
    export: function (id) {
        const db = this._dbs[id];
        if (!db) {
            throw new Error(`Database not found: ${id}`);
        }

        try {
            return db.export();
        } catch (error) {
            console.error('[SqlJs] Export failed:', error);
            throw error;
        }
    },

    /**
     * Close and remove a database.
     * @param {string} id - Database ID
     */
    close: function (id) {
        const db = this._dbs[id];
        if (db) {
            try {
                db.close();
            } catch (e) {
                // Ignore close errors
            }
            delete this._dbs[id];
            console.log(`[SqlJs] Closed database: ${id}`);
        }
    },

    /**
     * Close all open databases.
     */
    closeAll: function () {
        for (const id of Object.keys(this._dbs)) {
            this.close(id);
        }
    },

    /**
     * Generate a unique database ID.
     * @returns {string} UUID v4
     */
    _generateId: function () {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
            const r = Math.random() * 16 | 0;
            const v = c === 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }
};
