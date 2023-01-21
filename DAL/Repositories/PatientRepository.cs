﻿using BCrypt.Net;
using Common.DTO;
using Common.Models;
using DAL.Models;
using Microsoft.Azure.Cosmos;

namespace DAL.Repositories
{
    public class PatientRepository
    {
        private Database database;
        private Container container;

        public PatientRepository(CosmosClient client)
        {
            database = client.GetDatabase("Swasthya");
            container = database.GetContainer("Patient");
        }

        public async Task<Boolean> EmailExistsAsync(string email)
        {
            var query = $"SELECT * FROM Patient WHERE Patient.email = @email";
            var queryDefinition = new QueryDefinition(query).WithParameter("@email", email);
            var emailResponse = this.container.GetItemQueryIterator<PatientData>(queryDefinition);
            var response = await emailResponse.ReadNextAsync();
            if (response.Resource.FirstOrDefault() == null)
            {
                return false;
            }

            return true;
        }

        private string CreatePasswordHash(string password)
        {
            password = BCrypt.Net.BCrypt.EnhancedHashPassword(password, hashType: HashType.SHA512);
            return password;
        }

        private bool VerifyPasswordHash(string passwordInput, string passwordOriginal)
        {
            return BCrypt.Net.BCrypt.EnhancedVerify(passwordInput, passwordOriginal, hashType: HashType.SHA512);
        }

        public async Task<PatientData> CreatePatientAsync(string email, string password, string name, string phoneNumber, string dateOfBirth)
        {
            var patient = new PatientData()
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                Password = CreatePasswordHash(password),
                Name = name,
                PhoneNumber = phoneNumber,
                DateOfBirth = dateOfBirth
            };

            var patientCreated = await container.CreateItemAsync<PatientData>(patient);
            return patientCreated.Resource;
        }

        public async Task<IPatient> LoginPatientAsync(string email, string password)
        {
            var query = $"SELECT * FROM Patient WHERE Patient.email = @email";
            var queryDefinition = new QueryDefinition(query).WithParameter("@email", email);
            var patientResponse = this.container.GetItemQueryIterator<PatientData>(queryDefinition);
            var response = await patientResponse.ReadNextAsync();
            if (response.Resource.FirstOrDefault() == null)
            {
                return null;
            }

            if (!VerifyPasswordHash(password, response.Resource.FirstOrDefault().Password))
            {
                return null;
            }

            var patient = new Patient()
            {
                Name = response.Resource.FirstOrDefault().Name,
                Email = response.Resource.FirstOrDefault().Email,
                PhoneNumber = response.Resource.FirstOrDefault().PhoneNumber,
                DateOfBirth = response.Resource.FirstOrDefault().DateOfBirth,
            };

            return patient;
        }
    }
}
