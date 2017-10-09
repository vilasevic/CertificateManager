﻿using CertificateManager.Entities;
using LiteDB;

namespace CertificateManager.Repository
{
    public class LiteDbAuditRepository : IAuditRepository
    {
        private string auditCollectionName = "audit";
        private LiteDatabase db;

        public LiteDbAuditRepository(string path)
        {
            db = new LiteDatabase(path);
        }

        public void InsertAuditEvent(AuditEvent entity)
        {
            LiteCollection<AuditEvent> col =  db.GetCollection<AuditEvent>(auditCollectionName);
            col.Insert(entity);
        }
    }
}