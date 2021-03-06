﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PersonalBlog.DataAccess.Interfaces;
using PersonalBlog.DataAccess.Models;
using PersonalBlog.Services.Dto;
using PersonalBlog.Services.Exceptions;
using PersonalBlog.Services.Filters;
using PersonalBlog.Services.Helpers;
using PersonalBlog.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PersonalBlog.Services.Implementation
{
    public class UserService : Service<User, UserDto, UserFilter>, IUserService
    {
        private IConfiguration _configuration;

        public UserService(IUnitOfWork unitOfWork, IConfiguration configuration) :
            base(unitOfWork)
        {
            _configuration = configuration;
        }

        public void Subscribe(bool action, string id)
        {
            User entity = Repository
                .Get(e => e.Id == id)
                .SingleOrDefault();

            if (entity == null)
            {
                throw new ObjectNotFoundException();
            }

            entity.IsSubscribed = action;
            Repository.Update(entity);
            _unitOfWork.SaveChanges();
        }

        public override UserDto Get(string id)
        {
            User entity = Repository
              .Get(e => e.Id == id)
              .Include(u => u.UserRoles)
              .ThenInclude(ur => ur.Role)
              .SingleOrDefault();

            if (entity == null)
            {
                throw new ObjectNotFoundException();
            }

            return MapToDto(entity);
        }

        public override IEnumerable<UserDto> Get(UserFilter filter)
        {
            Func<User, bool> predicate = GetFilter(filter);
            List<User> entities = Repository
              .Get(p => predicate(p))
              .ToList();

            if (!entities.Any())
            {
                throw new ObjectNotFoundException();
            }

            return entities.Select(e => MapToDto(e));
        }

        public UserDto Get(string name, string password)
        {
            string hashPassword = UserHelper.GetPasswordHash(password);
            User entity = Repository
                .Get(u => u.Name == name && hashPassword == u.PasswordHash)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .SingleOrDefault();

            if (entity == null)
            {
                throw new ObjectNotFoundException();
            }

            return MapToDto(entity);
        }

        public override void Add(UserDto dto)
        {
            User checkEntity = Repository
                .Get(u => u.Name == dto.Name || u.Id == dto.Id)
                .SingleOrDefault();

            if (checkEntity != null)
            {
                throw new DuplicateNameException();
            }

            dto.PasswordHash = UserHelper.GetPasswordHash(dto.Password);
            User entity = MapToEntity(dto);

            var featuresNumber = int.Parse(_configuration["Data:FeaturesNumber"]);
            entity.Weights = Enumerable.Repeat(0.0, featuresNumber)
                .ToArray();

            Repository.Add(entity);
            _unitOfWork.SaveChanges();
        }

        public override void Remove(string id)
        {
            User entity = Repository
             .Get(e => e.Id == id)
             .SingleOrDefault();

            if (entity == null)
            {
                throw new ObjectNotFoundException();
            }

            Repository.Remove(entity);
            _unitOfWork.SaveChanges();
        }

        public override void Update(UserDto dto)
        {
            User entity = Repository
                .Get(e => e.Id == dto.Id)
                .SingleOrDefault();

            if (entity == null)
            {
                throw new ObjectNotFoundException();
            }

            entity.Name = dto.Name;
            entity.PasswordHash = dto.PasswordHash;
            entity.Email = dto.Email;
            entity.IsSubscribed = dto.IsSubscribed;


            Repository.Update(entity);
            _unitOfWork.SaveChanges();
        }

        protected override UserDto MapToDto(User entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException();
            }

            UserDto dto = new UserDto
            {
                Id = entity.Id,
                Name = entity.Name,
                PasswordHash = entity.PasswordHash,
                Email = entity.Email,
                IsSubscribed = entity.IsSubscribed,
                RoleNames = entity.UserRoles.Select(ur => ur.Role.Name).ToList()
            };

            return dto;
        }

        protected override User MapToEntity(UserDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException();
            }

            User entity = new User
            {
                Id = dto.Id,
                Name = dto.Name,
                PasswordHash = dto.PasswordHash,
                Email = dto.Email,
                IsSubscribed = dto.IsSubscribed
            };

            return entity;
        }

        private Func<User, bool> GetFilter(UserFilter filter)
        {
            Func<User, bool> result = e => true;
            if (!String.IsNullOrEmpty(filter?.Name))
            {
                result += e => e.Name == filter.Name;
            }

            return result;
        }
    }
}
