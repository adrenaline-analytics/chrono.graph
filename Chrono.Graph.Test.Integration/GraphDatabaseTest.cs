using AutoFixture;
using Chrono.Graph.Adapter.Neo4j;
using Chrono.Graph.Core.Application;
using Chrono.Graph.Core.Constant;
using Chrono.Graph.Core.Utilities;
using System.Diagnostics;

namespace Chrono.Graph.Test.Integration
{
    public class GraphDatabaseTest : BaseTest
    {
        public GraphDatabaseTest() : base() { }

        [Fact]
        public async Task OrganizationFacilitiesAndHeadquartersSaveOkay()
        {
            var id = Id();
            var timestamp = DateTime.UtcNow;
            var notes = $"test-{id}-{timestamp}";
            var locationA = new FacilityUnit
            {
                Id = Id(),
                Title = "Facility A",
                Notes = notes,
                CreatedAt = timestamp,
                ModifiedAt = timestamp,
            }; 

            var org = new EntityHub
            {
                Id = id,
                Notes = notes,
                CreatedAt = timestamp,
                ModifiedAt = timestamp,
                Headquarters = locationA,
                Locations = new List<FacilityUnit> { 
                    locationA
                },
            };

            await _db.Post(org, j => j
                .Join<EntityHub, FacilityUnit>(o => o.Headquarters)
                .Join<EntityHub, IList<FacilityUnit>>(o => o.Locations));

            var result = await _db.GetScalar<EntityHub>(
                q => q.Where<EntityHub, string>(o => o.Id, Is.Equal(id)),
                j => j.Join<EntityHub, IList<FacilityUnit>>(o => o.Locations)
                    .Join<EntityHub, FacilityUnit>(o => o.Headquarters)
                );


            Assert.NotNull(result);
            Assert.NotNull(result.Locations);
            Assert.NotNull(result.Headquarters);
            Assert.NotEmpty(result.Locations);
            Assert.Equal(id, result.Id);
            Assert.Equal(org.Locations.Count, result.Locations.Count);
            Assert.Equal(org.Locations.First().Title, result.Locations.First().Title);
            Assert.Equal(org.Locations.First().Notes, result.Locations.First().Notes);
            Assert.Equal(org.Headquarters.Title, result.Headquarters.Title);
            Assert.Equal(org.Headquarters.Notes, result.Headquarters.Notes);

            var newName = "Rename";
            result.Headquarters.Title = newName;

            await _db.Patch(result, j => j.Join<EntityHub, IList<FacilityUnit>>(o => o.Locations)
                    .Join<EntityHub, FacilityUnit>(o => o.Headquarters));

            var newresult = await _db.GetScalar<EntityHub>(
                q => q.Where<EntityHub, string>(o => o.Id, Is.Equal(id)),
                j => j.Join<EntityHub, IList<FacilityUnit>>(o => o.Locations)
                    .Join<EntityHub, FacilityUnit>(o => o.Headquarters)
                );

            Assert.NotNull(newresult);
            Assert.NotNull(newresult.Locations);
            Assert.NotNull(newresult.Headquarters);
            Assert.NotEmpty(newresult.Locations);
            Assert.Equal(id, newresult.Id);
            Assert.Equal(result.Locations.Count, newresult.Locations.Count);
            Assert.Equal(newName, newresult.Locations.First().Title);
            Assert.Equal(newName, newresult.Headquarters.Title);

        }
        [Fact]
        public async Task WhenOneObjectIsRetrievedMultipleTimesUseOneInstanceForAll()
        {
            var id = Id();
            var timestamp = DateTime.UtcNow;
            var notes = $"test-{id}-{timestamp}";
            
            var facilityA = new FacilityUnit
            {
                Id = Id(),
                Title = "Facility A",
                Notes = notes,
                CreatedAt = timestamp,
                ModifiedAt = timestamp,
            };

            var entity = new EntityHub
            {
                Id = id,
                Notes = notes,
                CreatedAt = timestamp,
                ModifiedAt = timestamp,
                Headquarters = facilityA,
                Locations = new List<FacilityUnit> { facilityA },
            };

            await _db.Post(entity, j => j.Join<EntityHub, IList<FacilityUnit>>(o => o.Locations)
                .Join<EntityHub, FacilityUnit>(o => o.Headquarters));
            var result = await _db.GetScalar<EntityHub>(
                q => q.Where<EntityHub, string>(o => o.Id, Is.Equal(id)),
                j => j.Join<EntityHub, IList<FacilityUnit>>(o => o.Locations)
                    .Join<EntityHub, FacilityUnit>(o => o.Headquarters)
            );

            Assert.NotNull(result);
            Assert.NotNull(result.Locations);
            Assert.NotNull(result.Headquarters);
            Assert.Single(result.Locations);
            Assert.Equal(result.Headquarters, result.Locations.First());
            result.Headquarters.Title = "adskjhfkalsufih322";
            Assert.Equal(result.Headquarters.Title, result.Locations.First().Title);
        }
        [Fact]
        public async Task OrganizationRoleAccessSavesWithKeyLabelling()
        {
            var id = Id();
            var phone = "888coolguy";
            var email = "888@cool.guy";
            var timestamp = DateTime.UtcNow;
            var notes = $"test-{id}-{timestamp}";
            var addressHq = TestUtils.MakeAddress();
            
            var liaisonContact = new CommunicationDetail
            {
                Id = Id(),
                ContactNumber = "605 Liaison Contact lol",
                CreatedAt = timestamp,
                ModifiedAt = timestamp,
                Notes = notes,
                VerifiedAt = timestamp,
                CountryPrefix = 1,
                CanText = true,
                AlertSettings = new List<AlertPreference>
                {
                    new AlertPreference
                    {
                        Id = Id(),
                        CreatedAt = timestamp,
                        ModifiedAt = timestamp,
                        Notes = notes,
                        Tag = "All notifications",
                        Description = "Could you describe the ruckus, sir?"
                    }
                }
            };

            var chipmunk = new UserIdentity
            {
                Id = Id(),
                ProfileData = new IndividualProfile
                {
                    Id = Id(),
                    First = "Chipmunk",
                    Last = "Habertonfaber",
                    Notes = notes,
                    CreatedAt = timestamp,
                    ModifiedAt = timestamp,
                },
                EmailAddress = email,
                Notes = notes,
                CreatedAt = timestamp,
                ModifiedAt = timestamp,
            };

            var facilityA = new FacilityUnit
            {
                Id = Id(),
                Title = "Facility A",
                Address = addressHq,
            };

            var entity = new EntityHub
            {
                Id = id,
                Notes = notes,
                CreatedAt = timestamp,
                ModifiedAt = timestamp,
                Headquarters = facilityA,
                Locations = new List<FacilityUnit> { facilityA },
                FinancialSource = new PaymentCredential
                {
                    Id = Id(),
                    CardOwner = "Chris King",
                    CardDigits = "1422024911924101",
                    SecurityCode = 114,
                    Notes = notes,
                    CreatedAt = timestamp,
                    ModifiedAt = timestamp,
                },
                Permissions = new Dictionary<UserRole, IList<UserIdentity>>
                {
                    {
                        UserRole.Supervisor, new List<UserIdentity>
                        {
                            chipmunk,
                            new UserIdentity
                            {
                                Id = Id(),
                                ProfileData = new IndividualProfile
                                {
                                    Id = Id(),
                                    First = "Beau",
                                    Last = "Wilson",
                                    Notes = notes,
                                    CreatedAt = timestamp,
                                    ModifiedAt = timestamp,
                                },
                                EmailAddress = phone,
                                Notes = notes,
                                CreatedAt = timestamp,
                                ModifiedAt = timestamp,
                            }
                        }
                    },
                    {
                        UserRole.Operator, new List<UserIdentity>
                        {
                            chipmunk,
                            new UserIdentity
                            {
                                Id = Id(),
                                ProfileData = new IndividualProfile
                                {
                                    Id = Id(),
                                    First = "Calipso",
                                    Notes = notes,
                                    CreatedAt = timestamp,
                                    ModifiedAt = timestamp,
                                },
                                EmailAddress = "miss@terio.us",
                                Notes = notes,
                                CreatedAt = timestamp,
                                ModifiedAt = timestamp,
                            }
                        }
                    }
                },
                ContactPoints = new Dictionary<ContactCategory, CommunicationDetail>
                {
                    { ContactCategory.TeamLiaison, liaisonContact },
                    { ContactCategory.OfficeSupport, liaisonContact }
                }
            };

            var filter = new Func<IJoiner, IJoiner>(
                j => j.Join<EntityHub, Dictionary<UserRole, IList<UserIdentity>>>(o => o.Permissions)
                    .Join<EntityHub, IList<FacilityUnit>>(o => o.Locations)
                    .Join<EntityHub, PaymentCredential?>(o => o.FinancialSource)
                    .Join<EntityHub, FacilityUnit>(o => o.Headquarters)
                    .Join<EntityHub, Dictionary<ContactCategory, CommunicationDetail>>(o => o.ContactPoints,
                        contactField => contactField.JoinOptional<CommunicationDetail, IList<AlertPreference>>(c => c.AlertSettings)));
            await _db.Post(entity, j => filter(j));

            var result = await _db.GetScalar<EntityHub>(
                q => q.Where<EntityHub, string>(o => o.Id, Is.Equal(id)), j => filter(j));

            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
            Assert.Equal(entity.Permissions.Count, result.Permissions.Count);
            Assert.Equal(entity.Permissions[UserRole.Supervisor].First().EmailAddress, result.Permissions[UserRole.Supervisor].First().EmailAddress);

            Assert.NotNull(result.ContactPoints);
            Assert.Equal(entity.ContactPoints.Count, result.ContactPoints.Count);
            Assert.Equal(entity.ContactPoints[ContactCategory.TeamLiaison].Id, result.ContactPoints[ContactCategory.TeamLiaison].Id);
            Assert.NotNull(result.ContactPoints[ContactCategory.TeamLiaison].AlertSettings);
            Assert.Equal(entity.ContactPoints[ContactCategory.TeamLiaison].AlertSettings.First().Tag, result.ContactPoints[ContactCategory.TeamLiaison].AlertSettings.First().Tag);

            Assert.Equal(entity.ContactPoints[ContactCategory.OfficeSupport].Id, result.ContactPoints[ContactCategory.OfficeSupport].Id);
            Assert.NotNull(result.ContactPoints[ContactCategory.OfficeSupport].AlertSettings);
            Assert.Equal(entity.ContactPoints[ContactCategory.OfficeSupport].AlertSettings.First().Tag, result.ContactPoints[ContactCategory.OfficeSupport].AlertSettings.First().Tag);

            Assert.NotEmpty(result.Locations);

            result.ContactPoints[ContactCategory.PartnerRelations] = liaisonContact;
            await _db.Put(result, j => j.Join<EntityHub, Dictionary<ContactCategory, CommunicationDetail>>(
                o => o.ContactPoints, j2 => j2.JoinOptional<CommunicationDetail, IList<AlertPreference>>(c => c.AlertSettings)));

            var updatedResult = await _db.GetScalar<EntityHub>(
                q => q.Where<EntityHub, string>(o => o.Id, Is.Equal(id)),
                j => j.Join<EntityHub, Dictionary<ContactCategory, CommunicationDetail>>(
                    o => o.ContactPoints, j2 => j2.JoinOptional<CommunicationDetail, IList<AlertPreference>>(c => c.AlertSettings)));

            Assert.NotNull(updatedResult);
            Assert.NotNull(updatedResult.ContactPoints);
            Assert.Equal(result.ContactPoints[ContactCategory.TeamLiaison].Id, updatedResult.ContactPoints[ContactCategory.TeamLiaison].Id);
            Assert.NotNull(updatedResult.ContactPoints[ContactCategory.TeamLiaison].AlertSettings);
            Assert.Equal(result.ContactPoints[ContactCategory.PartnerRelations].Id, updatedResult.ContactPoints[ContactCategory.PartnerRelations].Id);
            Assert.NotNull(updatedResult.ContactPoints[ContactCategory.PartnerRelations].AlertSettings);

        }

        [Fact]
        public async Task PostDictionaryEdgeDefinitionsWithCustomLabels()
        {
            var thing = new HowDictionaryEdgesWork();
            var filter = new Func<IJoiner, IJoiner> (q => 
            {
                q.Join<HowDictionaryEdgesWork, Dictionary<EdgeLabeledEnum, Stuff>>(t => t.EdgeValStuff);
                q.Join<HowDictionaryEdgesWork, Dictionary<SimpleEnum, Stuff>>(t => t.EdgeDictStuff);
                q.Join<HowDictionaryEdgesWork, Dictionary<NormalEnum, Stuff>>(t => t.NormalStuff);
                return q;
            });

            await _db.Post(thing, j => filter(j));
            var oThing = await _db.Get<HowDictionaryEdgesWork>(q => q.Where<HowDictionaryEdgesWork, string>(t => t.Word, Is.Equal(thing.Word)), q => filter(q));
            Assert.NotNull(oThing.FirstOrDefault());
            Assert.Single(oThing);
            Assert.Equal(thing.Word, oThing.First().Word);
            Assert.All(oThing.First().NormalStuff, s => { });
        }


        [Fact]
        public async Task ObjectCanAddOneChildObjectWithTwoDifferentEdges()
        {
            var id = Id();
            var address = TestUtils.MakeAddress();
            var hq = new FacilityUnit
            {
                Id = Id(),
                Address = address
            };
            var org = new EntityHub
            {
                Id = id,
                Headquarters = hq,
                Locations = [hq]
            };
            await _db.Put(org);
        }
        [Fact]
        public async Task AddressesAreAppliedToOrgsCorrectlyWhenFacilitiesAreGiven()
        {
            var id = Id();
            var addressHq = TestUtils.MakeAddress();
            var hq = new FacilityUnit
            {
                Id = Id(),
                Address = addressHq,
            };
            var spotTwo = new FacilityUnit
            {
                Id = Id(),
                Address = TestUtils.MakeAddress(),
            };
            var org = new EntityHub
            {
                Id = id,
                Headquarters = hq,
                Locations = [hq, spotTwo]

            };
            var addressFilter = new Func<IJoiner, IJoiner>(j => j
                .Join<Address, Province?>(a => a.Province, d => d
                    .Join<Province, IList<Township>>(p => p.Townships)
                    .Join<Province, Country>(p => p.Country))
                .Join<Address, Township?>(a => a.City, d => d
                    .Join<Township, Province>(t => t.Province)
                    .Join<Township, Country>(t => t.Country)
                    .Join<Township, IList<ProvincialCode>>(c => c.ProvincialCodes))
                .Join<Address, ProvincialCode?>(a => a.ProvincialCode, d => d
                    .Join<ProvincialCode, IList<Province>>(c => c.Provinces)
                    .Join<ProvincialCode, IList<Township>>(c => c.Townships)
                    .Join<ProvincialCode, Country>(c => c.Country))
                .Join<Address, Country?>(a => a.Country, d => d
                    .Join<Country, IList<Province>>(c => c.Provinces)));

            var facilityFilter = new Func<IJoiner, IJoiner>(j => j
                .Join<FacilityUnit, Address>(c => c.Address, d => addressFilter(d)));

            var filter = new Func<IJoiner, IJoiner>(j => j
                .Join<EntityHub, IList<FacilityUnit>>(e => e.Locations, dd => facilityFilter(dd))
                .Join<EntityHub, FacilityUnit>(e => e.Headquarters, dd => facilityFilter(dd)));

            await _db.Put(org, j => filter(j));
            var dbResult = await _db.GetScalar<EntityHub>(q => q.Where<EntityHub, string>(e => e.Id, Is.Equal(id)), j => filter(j));

            Assert.NotNull(dbResult);
            Assert.NotNull(dbResult.Headquarters.Address);
            Assert.NotNull(dbResult.Headquarters.Address.Province);
            Assert.NotNull(dbResult.Headquarters.Address.Province.Country);
            Assert.Single (dbResult.Headquarters.Address.Province.Country.Provinces ?? []);

            Assert.NotEmpty(dbResult.Locations);

            foreach(var loc in dbResult.Locations)
            {
                Assert.NotNull(loc);
                Assert.NotNull(loc.Address);
                Assert.NotNull(loc.Address.Province);
                Assert.NotNull(loc.Address.Province.Country);
                Assert.Single (loc.Address.Province.Country.Provinces ?? []);
            }
        }
        [Fact]
        public async Task AddressChildObjectsFullyEdgeTogether()
        {
            var address = TestUtils.MakeAddress();
            //await _db.Put(address, j => j.JoinAllChildrenRecursive(15));
            await _db.Put(address, j => j
                .JoinOptional<Address, Township?>(a => a.City, d => d
                    .JoinOptional<Township, Country?>(t => t.Country))
                .JoinOptional<Address, Province?>(a => a.Province, d => d
                    .JoinOptional<Province, IList<Township>?>(p => p.Townships)));

        }
        [Fact]
        public async Task RaceRepositoryIsAbleToStoreAndDeleteARace()
        {
            var id = Id();
            var node = _fixture.Create<NodeBase>();
            node.Id = id;
            node.Notes = id;
            await _db.Put(node);
            var savedNode = await _db.GetScalar<NodeBase>(q => q.Where<NodeBase, string>(n => n.Id, Is.Equal(id)));
            Assert.NotNull(savedNode);
            foreach (var prop in node.GetType().GetProperties())
            {
                if (!ObjectHelper.GetPrimitivity(prop).HasFlag(GraphPrimitivity.Object))
                {
                    Debug.WriteLine($"Checking {prop.Name} - {prop.GetValue(node)} == {prop.GetValue(savedNode)}");
                    Assert.Equal(prop.GetValue(node), prop.GetValue(savedNode));
                }
            }
            await DeleteTestData<NodeBase>(node.Notes);
            
        }
        [Fact]
        public async Task RepositoryIsAbleToSaveJoins()
        {

            var id = Id();
            var node = _fixture.Create<NodeBase>();
            node.Id = id;
            node.Notes = id;

            await _db.Put(node, j => j.Join<NodeBase, NodeSubclass>(b => b.NormalClass));

            var savedNode = await _db.GetScalar<NodeBase>(q => q.Where<NodeBase, string>(n => n.Id, Is.Equal(id)), j => j
                .JoinOptional<NodeBase, NodeSubclass?>(n => n.NormalClass)
                .JoinOptional<NodeBase, NodeSubclass?>(n => n.SubClass)
                .JoinOptional<NodeBase, IList<NodeSubclass>?>(n => n.NormalClasses)
            );
                
            Assert.NotNull(savedNode);
            Assert.NotNull(savedNode.NormalClass);
            Assert.Null(savedNode.SubClass);
            Assert.Empty(savedNode.NormalClasses ?? []);
        }
        [Fact]
        public async Task RepositoryIsAbleToSaveJoinsWithAllChildren()
        {

            var id = Id();
            var node = _fixture.Create<NodeBase>();
            node.Id = id;
            node.Notes = id;

            var filter = new Func<IJoiner, IJoiner>(j => j
                .JoinOptional<NodeBase, NodeSubclass?>(n => n.NormalClass)
                .JoinOptional<NodeBase, NodeSubclass?>(n => n.SubClass)
                .JoinOptional<NodeBase, IList<NodeSubclass>?>(n => n.NormalClasses)
            );
            await _db.Put(node, j => filter(j));
            var savedNode = await _db.GetScalar<NodeBase>(q => q.Where<NodeBase, string>(n => n.Id, Is.Equal(id)), j => filter(j));
                
            Assert.NotNull(savedNode);
            Assert.NotNull(savedNode.NormalClass);
            Assert.NotNull(savedNode.SubClass);
            Assert.NotEmpty(savedNode.NormalClasses ?? []);
        }
        [Fact]
        public async Task RepositoryIsAbleToSaveJoinsWithAllGrandChildren()
        {

            var id = Id();
            var node = _fixture.Create<NodeBase>();
            node.Id = id;
            node.Notes = id;
            var format = new Func<IJoiner, IJoiner>(j => j
                .JoinOptional<NodeBase, NodeSubclass?>(n => n.NormalClass, jj => jj
                    .JoinOptional<NodeSubclass, NodeSubSubclass>(nn => nn.InnerCoop)
                    .JoinOptional<NodeSubclass, IList<NodeSubSubclass>>(nn => nn.InnerCoops))
                .JoinOptional<NodeBase, NodeSubclass?>(n => n.SubClass)
                .JoinOptional<NodeBase, IList<NodeSubclass>?>(n => n.NormalClasses)
            );

            await _db.Put(node, j => format(j));
            var savedNode = await _db.GetScalar<NodeBase>(q => q.Where<NodeBase, string>(n => n.Id, Is.Equal(id)), j => format(j));
                
            Assert.NotNull(savedNode);
            Assert.NotNull(savedNode.NormalClass);
            Assert.NotEmpty(savedNode.NormalClass.InnerCoops);

            Assert.True(!string.IsNullOrEmpty(savedNode.NormalClass.InnerCoop.StoryBook));
            foreach(var coop in savedNode.NormalClass.InnerCoops)
            {
                Assert.NotNull(coop);
                Assert.True(!string.IsNullOrEmpty(coop.StoryBook));
            }

            Assert.NotNull(savedNode.SubClass);
            Assert.NotNull(savedNode.NormalClasses);
            Assert.NotEmpty(savedNode.NormalClasses);
        }
        [Fact]
        public async Task ChildrenWithChildrenArraysArePutAndRetrievedCorrectly()
        {
            var id = Id();
            var hq = new FacilityUnit
            {
                Id = Id(),
                Payees = [new () {
                    CardDigits = "8x",
                    Id = Id(),
                }, new () {
                    CardDigits = "92384290",
                    Id = Id()
                }]
            };
            var spotTwo = new FacilityUnit
            {
                Id = Id(),
                Payees = [new () {
                    CardDigits = "22",
                    Id = Id(),
                }, new () {
                    CardDigits = "sakldfjs",
                    Id = Id()
                }]
            };
            var org = new EntityHub
            {
                Id = id,
                Headquarters = hq,
                Locations = [hq, spotTwo]

            };
            var filter = new Func<IJoiner, IJoiner>(j => j
                .JoinOptional<EntityHub, IList<FacilityUnit>>(e => e.Locations, dd => dd
                    .JoinOptional<FacilityUnit, IList<PaymentCredential>>(c => c.Payees))
                .JoinOptional<EntityHub, FacilityUnit>(e => e.Headquarters, dd => dd
                    .JoinOptional<FacilityUnit, IList<PaymentCredential>>(c => c.Payees)));

            await _db.Put(org, j => filter(j));
            var dbResult = await _db.GetScalar<EntityHub>(q => q.Where<EntityHub, string>(e => e.Id, Is.Equal(id)), j => filter(j));

            Assert.NotNull(dbResult);
            Assert.NotEmpty(dbResult.Headquarters.Payees);
            Assert.NotEmpty(dbResult.Locations);

            foreach(var loc in dbResult.Locations)
                Assert.NotEmpty(loc.Payees);

        }
        [Fact]
        public async Task SameInstanceObjectButDifferentJoinFiltersOnDifferentProperties()
        {
            //same instance exists on two properties
            //one property has a join filter with no children
            //the other property has on child
            //combine them for this object only...
            var id = Id();
            var hq = new FacilityUnit
            {
                Id = Id(),
                Payees = [new () {
                    CardDigits = "8x",
                    Id = Id(),
                }, new () {
                    CardDigits = "92384290",
                    Id = Id()
                }]
            };
            var spotTwo = new FacilityUnit
            {
                Id = Id(),
                Payees = [new () {
                    CardDigits = "22",
                    Id = Id(),
                }, new () {
                    CardDigits = "sakldfjs",
                    Id = Id()
                }]
            };
            var org = new EntityHub
            {
                Id = id,
                Headquarters = hq,
                Locations = [hq, spotTwo]

            };
            var filter = new Func<IJoiner, IJoiner>(j => j
                .JoinOptional<EntityHub, IList<FacilityUnit>>(e => e.Locations, dd => dd
                    .JoinOptional<FacilityUnit, IList<PaymentCredential>>(c => c.Payees))
                .JoinOptional<EntityHub, FacilityUnit>(e => e.Headquarters, dd => dd
                    .JoinOptional<FacilityUnit, IList<PaymentCredential>>(c => c.Payees)));

            await _db.Put(org, j => filter(j));
            var dbResult = await _db.GetScalar<EntityHub>(q => q.Where<EntityHub, string>(e => e.Id, Is.Equal(id)), j => filter(j));

            Assert.NotNull(dbResult);
            Assert.NotEmpty(dbResult.Headquarters.Payees);
            Assert.NotEmpty(dbResult.Locations);

            foreach(var loc in dbResult.Locations)
                Assert.NotEmpty(loc.Payees);

        }
        [Fact]
        public async Task ChildrenWithChildrenArraysArePostedAndRetrievedCorrectly()
        {
            var id = Id();
            var hq = new FacilityUnit
            {
                Id = Id(),
                Payees = [new () {
                    CardDigits = "8x",
                    Id = Id(),
                }, new () {
                    CardDigits = "92384290",
                    Id = Id()
                }]
            };
            var spotTwo = new FacilityUnit
            {
                Id = Id(),
                Payees = [new () {
                    CardDigits = "22",
                    Id = Id(),
                }, new () {
                    CardDigits = "sakldfjs",
                    Id = Id()
                }]
            };
            var org = new EntityHub
            {
                Id = id,
                Headquarters = hq,
                Locations = [hq, spotTwo]

            };
            var filter = new Func<IJoiner, IJoiner>(j => j
                .JoinOptional<EntityHub, IList<FacilityUnit>>(e => e.Locations, dd => dd
                    .JoinOptional<FacilityUnit, IList<PaymentCredential>>(c => c.Payees))
                .JoinOptional<EntityHub, FacilityUnit>(e => e.Headquarters, dd => dd
                    .JoinOptional<FacilityUnit, IList<PaymentCredential>>(c => c.Payees)));

            await _db.Post(org, j => filter(j));
            var dbResult = await _db.GetScalar<EntityHub>(q => q.Where<EntityHub, string>(e => e.Id, Is.Equal(id)), j => filter(j));

            Assert.NotNull(dbResult);
            Assert.NotEmpty(dbResult.Headquarters.Payees);
            Assert.NotEmpty(dbResult.Locations);

            foreach(var loc in dbResult.Locations)
                Assert.NotEmpty(loc.Payees);

        }
        [Fact]
        public async Task ChildrenWithChildrenArraysArePatchedAndRetrievedCorrectly()
        {
            var id = Id();
            var hq = new FacilityUnit
            {
                Id = Id(),
                Payees = [new () {
                    CardDigits = "8x",
                    Id = Id(),
                }, new () {
                    CardDigits = "92384290",
                    Id = Id()
                }]
            };
            var spotTwo = new FacilityUnit
            {
                Id = Id(),
                Payees = [new () {
                    CardDigits = "22",
                    Id = Id(),
                }, new () {
                    CardDigits = "sakldfjs",
                    Id = Id()
                }]
            };
            var org = new EntityHub
            {
                Id = id,
                Headquarters = hq,
                Locations = [hq, spotTwo]

            };
            var filter = new Func<IJoiner, IJoiner>(j => j
                .JoinOptional<EntityHub, IList<FacilityUnit>>(e => e.Locations, dd => dd
                    .JoinOptional<FacilityUnit, IList<PaymentCredential>>(c => c.Payees))
                .JoinOptional<EntityHub, FacilityUnit>(e => e.Headquarters, dd => dd
                    .JoinOptional<FacilityUnit, IList<PaymentCredential>>(c => c.Payees)));

            await _db.Patch(org, j => filter(j));
            var dbResult = await _db.GetScalar<EntityHub>(q => q.Where<EntityHub, string>(e => e.Id, Is.Equal(id)), j => filter(j));

            Assert.NotNull(dbResult);
            Assert.NotEmpty(dbResult.Headquarters.Payees);
            Assert.NotEmpty(dbResult.Locations);

            foreach(var loc in dbResult.Locations)
                Assert.NotEmpty(loc.Payees);

        }
    }
}
