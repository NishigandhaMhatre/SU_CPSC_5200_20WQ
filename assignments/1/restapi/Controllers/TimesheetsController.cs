using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using restapi.Models;

namespace restapi.Controllers
{
    [Route("[controller]")]
    public class TimesheetsController : Controller
    {
        private readonly TimesheetsRepository repository;
       
        private readonly ILogger logger;

        public TimesheetsController(ILogger<TimesheetsController> logger)
        {
            repository = new TimesheetsRepository();
            this.logger = logger;
        }

        [HttpGet]
        [Produces(ContentTypes.Timesheets)]
        [ProducesResponseType(typeof(IEnumerable<Timecard>), 200)]
        public IEnumerable<Timecard> GetAll()
        {
            return repository
                .All
                .OrderBy(t => t.Opened);
        }

        [HttpGet("{id:guid}")]
        [Produces(ContentTypes.Timesheet)]
        [ProducesResponseType(typeof(Timecard), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetOne(Guid id)
        {
            logger.LogInformation($"Looking for timesheet {id}");

            Timecard timecard = repository.Find(id);

            if (timecard != null)
            {
                return Ok(timecard);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost("/timesheets")]   //Added path 
        [Produces(ContentTypes.Timesheet)]
        [ProducesResponseType(typeof(Timecard), 200)]
        public Timecard Create([FromBody] DocumentPerson person)
        {
            logger.LogInformation($"Creating timesheet for {person.ToString()}");

            var timecard = new Timecard(person.Id);

            var entered = new Entered() { Person = person.Id };

            timecard.Transitions.Add(new Transition(entered));

            repository.Add(timecard);

            return timecard;
        }

        [HttpDelete("{id:guid}/deletion")] // Added deletion to the path
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public IActionResult Delete(Guid id)
        {
            logger.LogInformation($"Looking for timesheet {id}");

            Timecard timecard = repository.Find(id);

            if (timecard == null)
            {
                return NotFound();
            }

            if (timecard.CanBeDeleted() == false)
            {
                return StatusCode(409, new InvalidStateError() { });
            }

            repository.Delete(id);

            return Ok();
        }

        [HttpGet("{id:guid}/lines")]
        [Produces(ContentTypes.TimesheetLines)]
        [ProducesResponseType(typeof(IEnumerable<TimecardLine>), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetLines(Guid id)
        {
            logger.LogInformation($"Looking for timesheet {id}");

            Timecard timecard = repository.Find(id);

            if (timecard != null)
            {
                var lines = timecard.Lines
                    .OrderBy(l => l.WorkDate)
                    .ThenBy(l => l.Recorded);

                return Ok(lines);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost("{id:guid}/lines")]
        [Produces(ContentTypes.TimesheetLine)]
        [ProducesResponseType(typeof(TimecardLine), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(InvalidStateError), 409)]
        public IActionResult AddLine(Guid id, [FromBody] DocumentLine documentLine)
        {
            logger.LogInformation($"Looking for timesheet {id}");

            Timecard timecard = repository.Find(id);

            if (timecard != null)
            {
                if (timecard.Status != TimecardStatus.Draft)
                {
                    return StatusCode(409, new InvalidStateError() { });
                }

                var annotatedLine = timecard.AddLine(documentLine);

                repository.Update(timecard);

                return Ok(annotatedLine);
            }
            else
            {
                return NotFound();
            }
        }

        //------------------------------------------------------------------------------------------------

        // Added route to getLine in a timecard
        [HttpGet("{id:guid}/lines/{lid:guid}")]
        [Produces(ContentTypes.TimesheetLine)]
        [ProducesResponseType(typeof(IEnumerable<Transition>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(InvalidStateError), 409)]
        public IActionResult GetLine(Guid id, Guid lid)
        {
            logger.LogInformation($"Looking for line {lid} in timesheet {id}");
            Timecard timecard = repository.Find(id);

            if (timecard != null)
            {
                
                var line = timecard.Lines
                    .Where(l => l.UniqueIdentifier == lid);
                  
                return Ok(line);
            }
            else
            {
                return NotFound();
            }
        }

        //Added route to Replace line with a new line in the timecard
        [HttpPost("{id:guid}/lines/{lid:guid}")]
        [Produces(ContentTypes.TimesheetLine)]
        [ProducesResponseType(typeof(IEnumerable<Transition>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(InvalidStateError), 409)]
        public IActionResult ReplaceLine(Guid id, Guid lid, [FromBody] DocumentLine documentLine)
        {
            logger.LogInformation($"Looking for line {lid} in timesheet {id}");
            Timecard timecard = repository.Find(id);

            if (timecard != null)
            {
                // ToDo: check for all status
                if (timecard.Status != TimecardStatus.Draft)
                {
                    return StatusCode(409, new InvalidStateError() { });
                }
                var line = timecard.Lines.Single(l => l.UniqueIdentifier == lid);
                timecard.Lines.Remove(line);

                var annotatedLine = timecard.ReplaceLine(documentLine , lid);

                repository.Update(timecard);

                return Ok(annotatedLine);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPatch("{id:guid}/lines/{lid:guid}")]
        [Produces(ContentTypes.TimesheetLine)]
        [ProducesResponseType(typeof(IEnumerable<Transition>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(InvalidStateError), 409)]
        public IActionResult UpdateLine(Guid id, Guid lid, [FromBody] DocumentLine documentLine)
        {
            logger.LogInformation($"Looking for line {lid} in timesheet {id}");
            Timecard timecard = repository.Find(id);

            if (timecard != null)
            {
                if (timecard.Status != TimecardStatus.Draft)
                {
                    return StatusCode(409, new InvalidStateError() { });
                }

                var line = timecard.Lines.Single(l => l.UniqueIdentifier == lid);
                timecard.Lines.Remove(line);

                var annotatedLine = timecard.UpdateLine(line, documentLine, lid);

                repository.Update(timecard);

                return Ok(annotatedLine);
            }
            else
            {
                return NotFound();
            }
        }


        [HttpGet("{id:guid}/transitions")]
        [Produces(ContentTypes.Transitions)]
        [ProducesResponseType(typeof(IEnumerable<Transition>), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetTransitions(Guid id)
        {
            logger.LogInformation($"Looking for timesheet {id}");

            Timecard timecard = repository.Find(id);

            if (timecard != null)
            {
                return Ok(timecard.Transitions);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost("{id:guid}/submittal")]
        [Produces(ContentTypes.Transition)]
        [ProducesResponseType(typeof(Transition), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(InvalidStateError), 409)]
        [ProducesResponseType(typeof(EmptyTimecardError), 409)]
        public IActionResult Submit(Guid id, [FromBody] Submittal submittal)
        {
            logger.LogInformation($"Looking for timesheet {id}");
            var submitter = submittal.Submitter;
            Timecard timecard = repository.Find(id);

            if (timecard != null)
            {
                // Check if the timecard employee and submitter are consistent
                if (timecard.Employee != submitter)
                {
                    return StatusCode(409, new InvalidEmployeeError() { });
                }
                if (timecard.Status != TimecardStatus.Draft)
                {
                    return StatusCode(409, new InvalidStateError() { });
                }

                if (timecard.Lines.Count < 1)
                {
                    return StatusCode(409, new EmptyTimecardError() { });
                }

                var transition = new Transition(submittal, TimecardStatus.Submitted);

                logger.LogInformation($"Adding submittal {transition}");

                timecard.Transitions.Add(transition);

                repository.Update(timecard);

                return Ok(transition);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("{id:guid}/submittal")]
        [Produces(ContentTypes.Transition)]
        [ProducesResponseType(typeof(Transition), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(MissingTransitionError), 409)]
        public IActionResult GetSubmittal(Guid id)
        {
            logger.LogInformation($"Looking for timesheet {id}");

            Timecard timecard = repository.Find(id);

            if (timecard != null)
            {
                if (timecard.Status == TimecardStatus.Submitted)
                {
                    var transition = timecard.Transitions
                                        .Where(t => t.TransitionedTo == TimecardStatus.Submitted)
                                        .OrderByDescending(t => t.OccurredAt)
                                        .FirstOrDefault();

                    return Ok(transition);
                }
                else
                {
                    return StatusCode(409, new MissingTransitionError() { });
                }
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost("{id:guid}/cancellation")]
        [Produces(ContentTypes.Transition)]
        [ProducesResponseType(typeof(Transition), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(InvalidStateError), 409)]
        [ProducesResponseType(typeof(EmptyTimecardError), 409)]
        public IActionResult Cancel(Guid id, [FromBody] Cancellation cancellation)
        {
            logger.LogInformation($"Looking for timesheet {id}");
            var canceler = cancellation.Canceler;
            Timecard timecard = repository.Find(id);

            if (timecard != null)
            {
                if (timecard.Status != TimecardStatus.Draft && timecard.Status != TimecardStatus.Submitted)
                {
                    return StatusCode(409, new InvalidStateError() { });
                }
                // Employee can cancel his draft timecard
                if (timecard.Status == TimecardStatus.Draft)
                {
                    if (canceler != timecard.Employee)
                    {
                        return StatusCode(409, new InvalidEmployeeError() { });
                    }
                }
                // Employee cannot cancel his own submitted timecard
                if (timecard.Status == TimecardStatus.Submitted)
                {
                    if (canceler == timecard.Employee)
                    {
                        return StatusCode(409, new InvalidEmployeeError() { });
                    }
                }
                var transition = new Transition(cancellation, TimecardStatus.Cancelled);

                logger.LogInformation($"Adding cancellation transition {transition}");

                timecard.Transitions.Add(transition);

                repository.Update(timecard);

                return Ok(transition);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("{id:guid}/cancellation")]
        [Produces(ContentTypes.Transition)]
        [ProducesResponseType(typeof(Transition), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(MissingTransitionError), 409)]
        public IActionResult GetCancellation(Guid id)
        {
            logger.LogInformation($"Looking for timesheet {id}");

            Timecard timecard = repository.Find(id);

            if (timecard != null)
            {
                if (timecard.Status == TimecardStatus.Cancelled)
                {
                    var transition = timecard.Transitions
                                        .Where(t => t.TransitionedTo == TimecardStatus.Cancelled)
                                        .OrderByDescending(t => t.OccurredAt)
                                        .FirstOrDefault();

                    return Ok(transition);
                }
                else
                {
                    return StatusCode(409, new MissingTransitionError() { });
                }
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost("{id:guid}/rejection")]
        [Produces(ContentTypes.Transition)]
        [ProducesResponseType(typeof(Transition), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(InvalidStateError), 409)]
        [ProducesResponseType(typeof(EmptyTimecardError), 409)]
        public IActionResult Reject(Guid id, [FromBody] Rejection rejection)
        {
            logger.LogInformation($"Looking for timesheet {id}");
            var rejecter = rejection.Rejecter;
            Timecard timecard = repository.Find(id);

            if (timecard != null)
            {
                //employee cannot reject his own timecard
                if (rejecter == timecard.Employee)
                {
                    return StatusCode(409, new InvalidEmployeeError() { });
                }
                if (timecard.Status != TimecardStatus.Submitted)
                {
                    return StatusCode(409, new InvalidStateError() { });
                }

                var transition = new Transition(rejection, TimecardStatus.Rejected);

                logger.LogInformation($"Adding rejection transition {transition}");

                timecard.Transitions.Add(transition);

                repository.Update(timecard);

                return Ok(transition);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("{id:guid}/rejection")]
        [Produces(ContentTypes.Transition)]
        [ProducesResponseType(typeof(Transition), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(MissingTransitionError), 409)]
        public IActionResult GetRejection(Guid id)
        {
            logger.LogInformation($"Looking for timesheet {id}");

            Timecard timecard = repository.Find(id);

            if (timecard != null)
            {
                if (timecard.Status == TimecardStatus.Rejected)
                {
                    var transition = timecard.Transitions
                                        .Where(t => t.TransitionedTo == TimecardStatus.Rejected)
                                        .OrderByDescending(t => t.OccurredAt)
                                        .FirstOrDefault();

                    return Ok(transition);
                }
                else
                {
                    return StatusCode(409, new MissingTransitionError() { });
                }
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost("{id:guid}/approval")]
        [Produces(ContentTypes.Transition)]
        [ProducesResponseType(typeof(Transition), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(InvalidStateError), 409)]
        [ProducesResponseType(typeof(EmptyTimecardError), 409)]
        public IActionResult Approve(Guid id, [FromBody] Approval approval)
        {
            logger.LogInformation($"Looking for timesheet {id}");
            var approver = approval.Approver;
            Timecard timecard = repository.Find(id);

            if (timecard != null)
            {
                // timecard employee cannot approve his own timecard.
                if (approver == timecard.Employee)
                {
                    return StatusCode(409, new InvalidEmployeeError() { });
                }
                if (timecard.Status != TimecardStatus.Submitted)
                {
                    return StatusCode(409, new InvalidStateError() { });
                }

                var transition = new Transition(approval, TimecardStatus.Approved);

                logger.LogInformation($"Adding approval transition {transition}");

                timecard.Transitions.Add(transition);

                repository.Update(timecard);

                return Ok(transition);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("{id:guid}/approval")]
        [Produces(ContentTypes.Transition)]
        [ProducesResponseType(typeof(Transition), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(MissingTransitionError), 409)]
        public IActionResult GetApproval(Guid id)
        {
            logger.LogInformation($"Looking for timesheet {id}");

            Timecard timecard = repository.Find(id);

            if (timecard != null)
            {
                if (timecard.Status == TimecardStatus.Approved)
                {
                    var transition = timecard.Transitions
                                        .Where(t => t.TransitionedTo == TimecardStatus.Approved)
                                        .OrderByDescending(t => t.OccurredAt)
                                        .FirstOrDefault();

                    return Ok(transition);
                }
                else
                {
                    return StatusCode(409, new MissingTransitionError() { });
                }
            }
            else
            {
                return NotFound();
            }
        }
    }
}
