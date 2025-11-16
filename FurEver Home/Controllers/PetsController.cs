using FurEver_Home.Models;
using System;
using System.Collections.Generic;

private static List<Pet> pets = new List<Pet>
{
    new Pet
    {
        PetId = 1,
        Name = "Parki",
        Type = "Dog",
        Breed = "Mixed Breed",
        Age = 2,
        Gender = "Male",
        Size = "Medium",
        Description = "Parki is a rescue dog that was found in a parking area. That's why Parki is his name!",
        ImageUrl = "/Content/Images/charlie.jpg",
        Traits = "Gentle,Quiet,Affectionate",
        Vaccines = "Anti Rabies, Dewormed, 5 in 1 Distemper, Hepatitis, Leptospirosis, Parvovirus, Parainfluenza (DHLPPi)",
        DaysInCenter = 60,
        WhyAdoptMe = "Healthy and neutered,Fully vaccinated,Deserves a loving forever home!",
        IsHealthy = true,
        IsNeutered = true,
        IsAdopted = false,
        DateAdded = DateTime.Now.AddDays(-60)
    },
    new Pet
    {
        PetId = 2,
        Name = "Max",
        Type = "Dog",
        Breed = "Golden Retriever",
        Age = 3,
        Gender = "Male",
        Size = "Large",
        Description = "Max is a friendly and energetic Golden Retriever who loves to play fetch and go for long walks.",
        ImageUrl = "/Content/Images/max.jpg",
        Traits = "Friendly,Energetic,Good with kids",
        Vaccines = "Anti Rabies, Dewormed, 5 in 1 Distemper, Hepatitis, Leptospirosis, Parvovirus, Parainfluenza (DHLPPi)",
        DaysInCenter = 45,
        WhyAdoptMe = "Great with children,Fully trained,Loves outdoor activities",
        IsHealthy = true,
        IsNeutered = true,
        IsAdopted = false,
        DateAdded = DateTime.Now.AddDays(-45)
    },
    new Pet
    {
        PetId = 3,
        Name = "Buddy",
        Type = "Dog",
        Breed = "Beagle",
        Age = 2,
        Gender = "Male",
        Size = "Medium",
        Description = "Buddy is a playful and curious beagle who loves exploring and making new friends.",
        ImageUrl = "/Content/Images/buddy.jpg",
        Traits = "Playful,Curious,Affectionate",
        Vaccines = "Anti Rabies, Dewormed, 5 in 1 Distemper, Hepatitis, Leptospirosis, Parvovirus, Parainfluenza (DHLPPi)",
        DaysInCenter = 30,
        WhyAdoptMe = "Perfect companion,Loves to play,Gets along with other pets",
        IsHealthy = true,
        IsNeutered = true,
        IsAdopted = false,
        DateAdded = DateTime.Now.AddDays(-30)
    },
    new Pet
    {
        PetId = 4,
        Name = "Bella",
        Type = "Cat",
        Breed = "Calico",
        Age = 1,
        Gender = "Female",
        Size = "Small",
        Description = "Bella is a playful and energetic calico cat who loves to chase toys and cuddle.",
        ImageUrl = "/Content/Images/bella.jpg",
        Traits = "Playful,Energetic,Curious",
        Vaccines = "Anti Rabies, Dewormed, Feline Distemper (FVRCP)",
        DaysInCenter = 20,
        WhyAdoptMe = "Affectionate lap cat,Low maintenance,Indoor friendly",
        IsHealthy = true,
        IsNeutered = true,
        IsAdopted = false,
        DateAdded = DateTime.Now.AddDays(-20)
    }
};