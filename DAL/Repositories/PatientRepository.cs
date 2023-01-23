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
            this.database = client.GetDatabase("Swasthya");
            this.container = database.GetContainer("Patient");
        }

        public async Task<Boolean> EmailExistsAsync(string email)
        {
            var query = $"SELECT * FROM Patient WHERE Patient.email = @email";
            var queryDefinition = new QueryDefinition(query).WithParameter("@email", email);
            var emailResponse = this.container.GetItemQueryIterator<PatientData>(queryDefinition);
            var response = await emailResponse.ReadNextAsync();

            return response.Resource.FirstOrDefault() != null;
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

        public async Task<PatientData> RegisterPatientAsync(string email, string password, string name, string phoneNumber, string dateOfBirth)
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

            var patientRegistered = await container.CreateItemAsync<PatientData>(patient);
            return patientRegistered.Resource;
        }

        public async Task<IPatient> LoginPatientAsync(string email, string password)
        {
            var query = $"SELECT * FROM Patient WHERE Patient.email = @email";
            var queryDefinition = new QueryDefinition(query).WithParameter("@email", email);
            var patientResponse = this.container.GetItemQueryIterator<PatientData>(queryDefinition);
            var response = await patientResponse.ReadNextAsync();
            var responseResource = response.Resource.FirstOrDefault();

            if (responseResource == null)
            {
                return null;
            }

            if (!VerifyPasswordHash(password, responseResource.Password))
            {
                return null;
            }

            var patient = new Patient()
            {
                Name = responseResource.Name,
                Email = responseResource.Email,
                PhoneNumber = responseResource.PhoneNumber,
                DateOfBirth = responseResource.DateOfBirth,
            };

            return patient;
        }
    }
}
